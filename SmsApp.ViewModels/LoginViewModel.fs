namespace SmsApp.ViewModels

open System
open System.Windows
open System.Windows.Controls
open FSharp.Data
open FSharp.Data.HttpRequestHeaders
open System.Windows.Input
open System.ComponentModel

open SmsApp.Models

type ViewModelBase() =
    let propertyChangedEvent = new DelegateEvent<PropertyChangedEventHandler>()
    interface INotifyPropertyChanged with
        [<CLIEvent>]
        member x.PropertyChanged = propertyChangedEvent.Publish
    member x.OnPropertyChanged propertyName = 
        propertyChangedEvent.Trigger([| x; new PropertyChangedEventArgs(propertyName) |])

type RelayCommand (canExecute:(obj -> bool), action:(obj -> unit)) =
    let event = new DelegateEvent<EventHandler>()
    interface ICommand with
        [<CLIEvent>]
        member x.CanExecuteChanged = event.Publish
        member x.CanExecute arg = canExecute(arg)
        member x.Execute arg = action(arg)

type LoginViewModel() = 
    inherit ViewModelBase()

    let authenticated = new Event<_>()

    let mutable model = { Url = ""; Username = ""; Status = "" }

    let GetUserDetails(password: string) = 
            { Url = model.Url; Name=model.Username; Password=password }

    let GetBasicHeader(loginDetails) = 
            sprintf "%s:%s" loginDetails.Name loginDetails.Password
            |> System.Text.ASCIIEncoding.ASCII.GetBytes
            |> System.Convert.ToBase64String
            |> (fun s -> "Basic " + s)

    let AuthenticateDetails(loginDetails: LoginDetails, status : string -> unit) =
        async {
            let auth = GetBasicHeader loginDetails
                    
            try
                status("Authenticating...")
                let! html = Http.AsyncRequestString("http://" + loginDetails.Url + "/v1.0/accounts", headers = [ Authorization auth ])
                status("Authenticated Successfully!")
                authenticated.Trigger(loginDetails)
            with
            | ex -> status("Unable to authenticate: " + ex.Message)
        }
        |> Async.Start

    member x.Url 
        with get () = model.Url
        and set value = model <- { model with Url = value }
                        x.OnPropertyChanged "Url"

    member x.Username 
        with get () = model.Username
        and set value = model <- { model with Username = value }
                        x.OnPropertyChanged "Username"

    member x.Status 
        with get () = model.Status
        and set value = model <- { model with Status = value }
                        x.OnPropertyChanged "Status"

    member x.Authenticate = 
        new RelayCommand ((fun canExecute -> true), (fun password -> 
            (password :?> PasswordBox).Password
            |> GetUserDetails
            |> (fun details -> (details, (fun status -> x.Status <- status)))
            |> AuthenticateDetails))

    member x.Authenticated = authenticated.Publish