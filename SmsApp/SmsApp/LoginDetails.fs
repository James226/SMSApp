namespace SMSApp

open System.Xml.Serialization 

[<CLIMutable>]
type Endpoint = {
    [<XmlElement("phonenumber")>]
    PhoneNumber: string
}

[<CLIMutable>]
[<XmlRoot("messageheader", Namespace = "http://api.esendex.com/ns/")>]
type MessageHeader = {
    [<XmlAttribute("id")>]
    Id: string
    [<XmlElement("reference", IsNullable = true)>]
    Reference: string
    [<XmlElement("status")>]
    Status: string
    [<XmlElement("laststatusat", IsNullable = true)>]
    LastStatusAt: string
    [<XmlElement("submittedat", IsNullable = true)>]
    SubmittedAt: string
    [<XmlElement("receivedat", IsNullable = true)>]
    ReceivedAt: string
    [<XmlElement("type")>]
    Type: string
    [<XmlElement("to")>]
    To: Endpoint
    [<XmlElement("from")>]
    From: Endpoint
    [<XmlElement("summary")>]
    Summary: string
    [<XmlElement("body")>]
    Body: string
    [<XmlElement("direction")>]
    Direction: string
    [<XmlElement("parts")>]
    Parts: string
    [<XmlElement("username", IsNullable = true)>]
    Username: string

}

[<CLIMutable>]
[<XmlRoot("message")>]
type MessageDetails = {
    [<XmlElement("from")>]
    From: string
    [<XmlElement("to")>]
    To: string
    [<XmlElement("body")>]
    Body: string
}

[<CLIMutable>]
[<XmlRoot("messages")>]
type MessageContainer = {
    [<XmlElement("accountreference")>]
    AccountReference: string
    [<XmlElement("message")>]
    Message : MessageDetails
}

[<CLIMutable>]
[<XmlRoot("messageheaders", Namespace = "http://api.esendex.com/ns/")>]
type MessageHeaders = {
    [<XmlElement("messageheader")>]
    MessageHeader: MessageHeader[]
}

[<CLIMutable>]
type InboxItem = {
    Id:string
    From:string
    Message:string
    ReceivedAt:string
    Account:string
}

[<CLIMutable>]
type Account = {
    [<XmlAttribute("id")>]
    Id: string
    [<XmlElement("reference")>]
    Reference: string
    [<XmlElement("label")>]
    Label: string
    [<XmlElement("address")>]
    Address: string
    [<XmlElement("type")>]
    Type: string
    [<XmlElement("messagesremaining")>]
    MessagesRemaining: string
    [<XmlElement("expireson")>]
    ExpiresOn: string
    [<XmlElement("role")>]
    Role: string
}

[<CLIMutable>]
[<XmlRoot("accounts", Namespace = "http://api.esendex.com/ns/")>]
type Accounts = {
    [<XmlElement("account")>]
    Account : Account[]
}

[<CLIMutable>]
[<XmlRoot("pushregistration", Namespace = "http://api.esendex.com/ns/")>]
type PushRegistration = {
    [<XmlAttribute("id")>]
    Id: string
    [<XmlElement("concurrencyid")>]
    ConcurrencyId: string
    [<XmlElement("accountid")>]
    AccountId: string
    [<XmlElement("pushurl")>]
    PushUrl: string
    [<XmlElement("type")>]
    Type: string
    [<XmlElement("displayname")>]
    DisplayName: string
}
