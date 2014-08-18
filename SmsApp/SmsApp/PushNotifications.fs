namespace SMSApp

open System
open System.Text
open System.Web.Http
open System.Net.Http
open System.Runtime.Serialization
open System.Web.Http.SelfHost 
open Ninject
open System.Web.Http.Dependencies
open WebApiContrib.IoC.Ninject

module PushNotifications =
    type aDefault = { id : obj }

    [<CLIMutable>]
    type InboundMessage = {
        Id: string
        MessageId: string
        AccountId: string
        MessageText: string
        From: string
        To: string
    }    

    [<CLIMutable>]
    type MessageDelivered = {
        Id: string
        MessageId: string
        AccountId: string
        OccurredAt: string
    }   

    [<CLIMutable>]
    type MessageFailed = {
        Id: string
        MessageId: string
        AccountId: string
        OccurredAt: string
    }

    [<CLIMutable>]
    type AccountEventHandlerOptions = {
        username: string
        password: string
        account: string
        notificationType: string
        id: string
        originator: string
        recipient: string
        body: string
        eventType: string
        sentAt: string
        receivedAt:string
        occurredAt: string
    }

    type Route = {
        id: RouteParameter
    }

    type PushNotificationConsumer =
        abstract member DoStuff: string -> unit
        abstract member MessageReceived: InboundMessage -> unit
        abstract member MessageDelivered: MessageDelivered -> unit
        abstract member MessageNotification: AccountEventHandlerOptions -> unit

    type MessageReceivedController(consumer: PushNotificationConsumer) =
        inherit ApiController()
            member x.Post(inboundMessage: InboundMessage) = 
                consumer.MessageReceived inboundMessage
                "Message Received"

    type MessageDeliveredController(consumer: PushNotificationConsumer) =
        inherit ApiController()
            member x.Post(messageDelivered: MessageDelivered) = 
                consumer.MessageDelivered messageDelivered
                "Message Delivered"

    type MessageFailedController(consumer: PushNotificationConsumer) =
        inherit ApiController()
            member x.Post(messageFailed: MessageFailed) = 
                consumer.DoStuff "Message Failed"
                "Message Failed"

    type AccountEventHandlerController(consumer: PushNotificationConsumer) =
        inherit ApiController()
            member x.Post(data: AccountEventHandlerOptions) =
                consumer.MessageNotification data
                "Account EventHandler"

    let CreateResolver(kernel) : IDependencyResolver = 
        new NinjectResolver(kernel) :> IDependencyResolver

    let Start consumer = 
        let config = new HttpSelfHostConfiguration( "http://localhost:8090/" )
        let kernel = new StandardKernel();
        kernel.Bind<PushNotificationConsumer>().ToConstant(consumer) |> ignore
      
        config.Formatters.XmlFormatter.UseXmlSerializer <- true
        config.DependencyResolver <- CreateResolver kernel
        config.Routes.MapHttpRoute("DefaultApi", "api/{controller}/{id}",
            {id = RouteParameter.Optional}) |> ignore
  
        
        let server = new HttpSelfHostServer(config)
        try
            server.OpenAsync() |> ignore
            printfn "The server is running..."
        with
        | ex -> printfn "%s" ex.InnerException.Message