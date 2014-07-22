namespace SMSApp

open System
open System.Windows
open System.Windows.Controls
open FSharpx
open FSharp.Data
open FSharp.Data.HttpRequestHeaders

type LoginModel = {
    Url: string
    Username: string
    Status: string
}

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

module Login =    
    type LoginWindow = XAML<"LoginWindow.xaml">

    let LoadWindow() =
        let loginWindow = LoginWindow()
        let loginViewModel = LoginViewModel()
        loginWindow.Root.DataContext <- loginViewModel

        let Authenticated(loginDetails: LoginDetails) =
            Application.Current.Dispatcher.Invoke (fun _ -> MainWindow(loginDetails).Open(); loginWindow.Root.Close())

        let authenticatedSub =
            loginViewModel.Authenticated
            |> Observable.subscribe Authenticated

        loginWindow.Root
