namespace SMSApp

open System
open System.Windows
open System.Windows.Controls
open FSharpx
open FSharp.Data
open FSharp.Data.HttpRequestHeaders
open System.Xml.Serialization 
open System.IO
open System.Linq
open SMSApp
open SMSApp.PushNotifications
open SmsApp.Models

type MainWindowXaml = XAML<"MainWindow.xaml">

type Consumer(window : MainWindowXaml) =
    let mainWindow = window
    interface PushNotificationConsumer with
        member x.DoStuff(stuff) =
            stuff |> ignore

        member x.MessageReceived(receivedMessage: InboundMessage) =
            Application.Current.Dispatcher.Invoke (fun _ -> 
                let inboxItem = { Id = receivedMessage.MessageId; From = receivedMessage.From; Message = receivedMessage.MessageText; ReceivedAt = DateTime.UtcNow.ToString(); Account = receivedMessage.AccountId }
                window.InboxTable.ItemsSource <- inboxItem :: (window.InboxTable.ItemsSource :?> InboxItem list)
                )

        member x.MessageDelivered(deliveredMessage) =
            Application.Current.Dispatcher.Invoke (fun _ -> 
                let currentId = window.MessageId.Text
                match deliveredMessage.MessageId with
                | currentId -> window.Status.Content <- "Delivered")

type SendSMSViewModel() =
    inherit ViewModelBase()

type MainWindow(loginDetails : LoginDetails) =
    let GetBasicHeader(loginDetails) = 
            sprintf "%s:%s" loginDetails.Name loginDetails.Password
            |> System.Text.ASCIIEncoding.ASCII.GetBytes
            |> System.Convert.ToBase64String
            |> (fun s -> "Basic " + s)

    let mutable dispatcher : SmsDispatcher = RestDispatcher(loginDetails) :> SmsDispatcher
    let mutable registeredNotifications : string list = []

    member x.SetDispatcher(dis: SmsDispatcher) =
        dispatcher <- dis

    member x.AddNotification(id: string) =
        registeredNotifications <- id :: registeredNotifications

    member x.GetNotifications() =
        registeredNotifications

    member x.Open() =
        let mainWindow = MainWindowXaml()
        let mcSerializer = XmlSerializer(typeof<MessageContainer>)
        let accountsSerializer = XmlSerializer(typeof<Accounts>)
        let smSerializer = XmlSerializer(typeof<MessageHeader>)
        let mdrSerializer = XmlSerializer(typeof<MessageHeaders>)
        let auth = GetBasicHeader loginDetails

        let GetMessageDetails() = 
            { AccountReference = mainWindow.AccountSelect.Text; Message = { From = mainWindow.From.Text; To = mainWindow.To.Text; Body = mainWindow.Message.Text } }

        let SerializeMessage(messageContainer, serializer : XmlSerializer) = 
            let ms = new System.IO.MemoryStream()
            serializer.Serialize(ms, messageContainer)
            ms.Seek(0L, System.IO.SeekOrigin.Begin) |> ignore 
            let reader = new StreamReader(ms)
            reader.ReadToEnd()

        let DeserializeMessageHeader(stream : Stream) =
            smSerializer.Deserialize stream :?> MessageHeader

        let DisplayMessageHeader(messageHeader : MessageHeader) =
            mainWindow.Status.Content <- messageHeader.Status
            mainWindow.Body.Content <- messageHeader.Summary

        let GetSentMessage(messageId) =
            System.Threading.Thread.Sleep(500)
            try
                let http = Http.RequestStream("http://" + loginDetails.Url + "/v1.0/messageheaders/" + messageId, headers = [ Authorization auth ])

                http.ResponseStream
                |> DeserializeMessageHeader
                |> DisplayMessageHeader
            with
            | ex -> MessageBox.Show(ex.Message) |> ignore

        let SendSerializedMessage(serializedMessage) =
            async {                    
                let! http = Http.AsyncRequestStream("http://" + loginDetails.Url + "/v1.0/messagedispatcher", headers = [ Authorization auth ], body = TextRequest serializedMessage)
                let response = mdrSerializer.Deserialize http.ResponseStream :?> MessageHeaders
                mainWindow.Root.Dispatcher.Invoke (fun _ -> 
                    GetSentMessage(response.MessageHeader.First().Id)
                    mainWindow.MessageId.Text <- response.MessageHeader.First().Id
                    mainWindow.TabControl.SelectedIndex <- 1)

            }
            |> Async.Start

        let GoToSentMessage(messageId) =
            match messageId with
            | "" -> MessageBox.Show("Unable to determine message id") |> ignore
            | _ -> GetSentMessage(messageId); mainWindow.MessageId.Text <- messageId; mainWindow.TabControl.SelectedIndex <- 1

        let SendMessage(messageContainer) =         
            messageContainer
            |> dispatcher.SendMessage
            |> GoToSentMessage
            
        let sendSub = 
            mainWindow.Send.Click
            |> Observable.map (fun _ -> GetMessageDetails())
            |> Observable.subscribe SendMessage

        let DeserializeAccounts(src : string) = 
            let bytes = System.Text.Encoding.ASCII.GetBytes(src)
            let stream = new MemoryStream(bytes)
            accountsSerializer.Deserialize stream :?> Accounts

        let DeserializePushRegistration(src : string) = 
            let serializer = XmlSerializer(typeof<PushRegistration>)
            let bytes = System.Text.Encoding.ASCII.GetBytes(src)
            let stream = new MemoryStream(bytes)
            serializer.Deserialize stream :?> PushRegistration

        let RegisterPushNotifications(accountId: string, notificationType: string) =
            let serializer = XmlSerializer(typeof<PushRegistration>)
            let registration = {
                Id = "";
                ConcurrencyId = "2c7d34fc-84a9-4f4d-9014-5b3173402903";
                AccountId = accountId;
                PushUrl = "http://10.1.6.16:8090/api/" + notificationType;
                Type = notificationType;
                DisplayName = "SMS App"
                }
            let message = SerializeMessage(registration, serializer)
            let response = DeserializePushRegistration(Http.RequestString("http://" + loginDetails.Url + "/v1.2/pushregistrations", headers = [ Authorization auth; ContentType HttpContentTypes.Xml ], body = TextRequest message))
            response.Id

        let PopulateAccountsList(accounts : Accounts) =
            mainWindow.AccountSelect.Items.Clear()
            for acct in accounts.Account do
                x.AddNotification(RegisterPushNotifications(acct.Id, "MessageDelivered"))
                x.AddNotification(RegisterPushNotifications(acct.Id, "MessageReceived"))
                mainWindow.AccountSelect.Items.Add(acct.Reference) |> ignore
            mainWindow.AccountSelect.SelectedIndex <- 0

        Http.RequestString("http://" + loginDetails.Url + "/v1.0/accounts", headers = [ Authorization auth ])
        |> DeserializeAccounts
        |> PopulateAccountsList

        let sentMessageSub =
            mainWindow.SentMessageSearch.Click
            |> Observable.map (fun _ -> mainWindow.MessageId.Text)
            |> Observable.subscribe GetSentMessage
        
        let DisplayInboxItems(items : MessageHeader[]) =
            if items <> null then
                mainWindow.InboxTable.ItemsSource <- [for item in items -> { Id = item.Id; From = item.From.PhoneNumber; Message = item.Summary; ReceivedAt = DateTime.Parse(item.ReceivedAt).ToString(); Account = item.Reference }]
                

        let GetInboxItems() = 
            async {                    
                let! http = Http.AsyncRequestStream("http://" + loginDetails.Url + "/v1.0/inbox/messages", headers = [ Authorization auth ])
                let response = mdrSerializer.Deserialize http.ResponseStream :?> MessageHeaders
                mainWindow.Root.Dispatcher.Invoke (fun _ -> DisplayInboxItems(response.MessageHeader))
            }
            |> Async.Start

        let inboxRefreshSub =
            mainWindow.RefreshInbox.Click
            |> Observable.subscribe (fun _ -> GetInboxItems())

        let ChangeProtocol(args) =
            let protocol = mainWindow.Protocol.SelectedItem :?> ComboBoxItem
            match protocol.Content.ToString() with
            | "REST" -> x.SetDispatcher(RestDispatcher loginDetails :> SmsDispatcher)
            | "Soap" -> x.SetDispatcher(SoapDispatcher loginDetails :> SmsDispatcher)
            | "FormPost" -> x.SetDispatcher(FormPostDispatcher loginDetails :> SmsDispatcher)
            | "SMPP" -> x.SetDispatcher(SMPPDispatcher (loginDetails, (fun message -> Application.Current.Dispatcher.Invoke (fun _ -> mainWindow.ConnectionStatus.Content <- message))) :> SmsDispatcher )
            | _ -> MessageBox.Show("Unknown Protocol: " + protocol.Content.ToString()) |> ignore

        let protocolSub =
            mainWindow.Protocol.SelectionChanged
            |> Observable.subscribe ChangeProtocol

        let OnClose() =
            let notifications = x.GetNotifications()
            for id in notifications do
                Http.Request("http://" + loginDetails.Url + "/v1.2/pushregistrations/" + id, httpMethod = HttpMethod.Delete, headers = [ Authorization auth; ContentType HttpContentTypes.Xml ]) |> ignore

        let closeSub = 
            mainWindow.Root.Closing 
            |> Observable.subscribe (fun _ -> OnClose())

        GetInboxItems()

        Consumer(mainWindow)
        |> PushNotifications.Start

        

        mainWindow.Root.Show()
        