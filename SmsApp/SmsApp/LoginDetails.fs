namespace SMSApp

open System.Xml.Serialization 


type LoginDetails = {
    Url: string
    Name: string
    Password: string
}

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
type InboxItem = {
    Id:string
    From:string
    Message:string
    ReceivedAt:string
    Account:string
}