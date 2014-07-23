namespace SmsApp.Models

type LoginModel = {
    Url: string
    Username: string
    Status: string
}

[<CLIMutable>]
type LoginDetails = {
    Url: string
    Name: string
    Password: string
}