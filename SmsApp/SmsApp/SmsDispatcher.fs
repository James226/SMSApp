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
                x.sendClient.SendMessage(messengerHeader, messageContainer.Message.To, messageContainer.Message.Body, sendService.ServiceTypes.MessageType.Text)
            with
                | :? ServerTooBusyException as exn ->
                    let innerMessage =
                        match (exn.InnerException) with
                        | null -> ""
                        | innerExn -> innerExn.Message
                    printfn "An exception occurred:\n %s\n %s" exn.Message innerMessage; ""
                | exn -> printfn "An exception occurred: %s" exn.Message; ""