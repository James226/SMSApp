namespace SMSApp

type SmsDispatcher =
    abstract member SendMessage: MessageContainer -> string

open System.IO
open System.Xml.Serialization 
open FSharp.Data
open FSharp.Data.HttpRequestHeaders

open SmsApp.Models

type RestDispatcher(loginDetails: LoginDetails) =
    let mcSerializer = XmlSerializer(typeof<MessageContainer>)
    let mdrSerializer = XmlSerializer(typeof<MessageHeaders>)

    let GetBasicHeader(loginDetails) = 
        sprintf "%s:%s" loginDetails.Name loginDetails.Password
        |> System.Text.ASCIIEncoding.ASCII.GetBytes
        |> System.Convert.ToBase64String
        |> (fun s -> "Basic " + s)

    let auth = GetBasicHeader loginDetails

    let SerializeMessage(messageContainer) = 
        let ms = new System.IO.MemoryStream()
        mcSerializer.Serialize(ms, messageContainer)
        ms.Seek(0L, System.IO.SeekOrigin.Begin) |> ignore 
        let reader = new StreamReader(ms)
        reader.ReadToEnd()

    let SendSerializedMessage(serializedMessage) =
        let http = Http.RequestStream("http://" + loginDetails.Url + "/v1.0/messagedispatcher", headers = [ Authorization auth ], body = TextRequest serializedMessage)
        let response = mdrSerializer.Deserialize http.ResponseStream :?> MessageHeaders
        response.MessageHeader.[0].Id  

    interface SmsDispatcher with
        member x.SendMessage messageContainer =
            messageContainer
            |> SerializeMessage
            |> SendSerializedMessage

open System
open System.Text.RegularExpressions
open System.ServiceModel
open Microsoft.FSharp.Linq
open Microsoft.FSharp.Data.TypeProviders

type sendService = WsdlService<"http://dev.esendex.com/secure/messenger/soap/SendService.asmx?wsdl">

type SoapDispatcher(loginDetails: LoginDetails) =
    member x.loginDetails = loginDetails
    member x.sendClient = sendService.GetSendServiceSoap()

    interface SmsDispatcher with
        member x.SendMessage messageContainer =
            try
                let messengerHeader = sendService.ServiceTypes.MessengerHeader(Account=messageContainer.AccountReference, Username=loginDetails.Name, Password=loginDetails.Password)
                x.sendClient.SendMessage(messengerHeader, messageContainer.Message.To, messageContainer.Message.Body, sendService.ServiceTypes.MessageType.Unicode)
            with
                | :? ServerTooBusyException as exn ->
                    let innerMessage =
                        match (exn.InnerException) with
                        | null -> ""
                        | innerExn -> innerExn.Message
                    printfn "An exception occurred:\n %s\n %s" exn.Message innerMessage; ""
                | exn -> printfn "An exception occurred: %s" exn.Message; ""

type FormPostDispatcher(loginDetails: LoginDetails) =
    let GetBasicHeader(loginDetails) = 
        sprintf "%s:%s" loginDetails.Name loginDetails.Password
        |> System.Text.ASCIIEncoding.ASCII.GetBytes
        |> System.Convert.ToBase64String
        |> (fun s -> "Basic " + s)

    let auth = GetBasicHeader loginDetails

    interface SmsDispatcher with
        member x.SendMessage messageContainer =
            let response = Http.RequestString("http://" + loginDetails.Url.Substring(4) + "/secure/messenger/formpost/SendSMS.aspx", body = FormValues [ "Username", loginDetails.Name; "Password", loginDetails.Password; "Account", messageContainer.AccountReference; "Recipient", messageContainer.Message.To; "Originator", messageContainer.Message.From; "Body", messageContainer.Message.Body; "PlainText", "1" ])
            let reg = Regex.Match(response, "MessageIDs=(.*)")
            match reg.Success with
            | true -> reg.Groups.[1].Captures.[0].Value
            | false -> ""

open System.Windows
open JamaaTech.Smpp.Net.Client
open JamaaTech.Smpp.Net.Lib
open JamaaTech.Smpp.Net.Lib.Protocol
open System.Linq

type SMPPDispatcher(loginDetails: LoginDetails, status: string -> unit) =
    let smppClient = new SmppClient()
    
    let Init() =
        smppClient.Properties.SystemID <- loginDetails.Name.Split('@').First()
        smppClient.Properties.Password <- loginDetails.Password
        smppClient.Properties.Port <- 30134
        smppClient.Properties.Host <- "smpp." + loginDetails.Url.Substring(4)
        smppClient.Properties.SystemType <- ""
        smppClient.Properties.DefaultServiceType <- ""
        
        
        smppClient.AutoReconnectDelay <- 3000
        smppClient.KeepAliveInterval <- 15000

        smppClient.Start()


    let smppStatusSub =
        smppClient.ConnectionStateChanged
        |> Observable.subscribe (fun args -> status("New State: " + args.CurrentState.ToString() + " .. Previous State: " + args.PreviousState.ToString()))

    do Init()

    interface SmsDispatcher with
        member x.SendMessage messageContainer =
            let submitSm = SubmitSm()
            submitSm.SourceAddress.Address <- messageContainer.Message.From
            submitSm.DestinationAddress.Address <- messageContainer.Message.To
            submitSm.DestinationAddress.Npi <- NumberingPlanIndicator.ISDN
            submitSm.DestinationAddress.Ton <- TypeOfNumber.International
            submitSm.SourceAddress.Npi <- NumberingPlanIndicator.ISDN
            submitSm.SourceAddress.Ton <- TypeOfNumber.International
            submitSm.EsmClass <- EsmClass.Default
            submitSm.RegisteredDelivery <- RegisteredDelivery.DeliveryReceipt
            submitSm.ServiceType <- ""
            submitSm.SetMessageText(messageContainer.Message.Body, DataCoding.SMSCDefault)

            let response = smppClient.CustomSendPDU(submitSm) :?> SubmitSmResp
            response.MessageID