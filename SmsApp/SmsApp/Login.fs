namespace SMSApp

open System
open System.Windows
open System.Windows.Controls
open FSharpx
open FSharp.Data
open FSharp.Data.HttpRequestHeaders

module Login =    
    type LoginWindow = XAML<"LoginWindow.xaml">

    let LoadWindow() =
        let loginWindow = LoginWindow()

        let GetUserDetails() = 
            { Url = loginWindow.Url.Text; Name=loginWindow.Username.Text; Password=loginWindow.Password.Password}

        let GetBasicHeader(loginDetails) = 
            sprintf "%s:%s" loginDetails.Name loginDetails.Password
            |> System.Text.ASCIIEncoding.ASCII.GetBytes
            |> System.Convert.ToBase64String
            |> (fun s -> "Basic " + s)

        let Authenticate(loginDetails : LoginDetails) = 
            async {
                Application.Current.Dispatcher.Invoke (fun _ -> loginWindow.Status.Text <- "Authenticating..."; loginWindow.Button.IsEnabled <- false)
                let auth = GetBasicHeader loginDetails
                    
                try
                    let! html = Http.AsyncRequestString("http://" + loginDetails.Url + "/v1.0/accounts", headers = [ Authorization auth ])
                    printfn "%d" html.Length
                   
                    Application.Current.Dispatcher.Invoke (fun _ -> MainWindow(loginDetails).Open(); loginWindow.Root.Close())
                with
                | :? Exception as ex -> Application.Current.Dispatcher.Invoke (fun _ ->  loginWindow.Status.Text <- "Failure: " + ex.Message; loginWindow.Button.IsEnabled <- true)
            }
            |> Async.Start

        let submitSub = 
            loginWindow.Button.Click
            |> Observable.map (fun _ -> GetUserDetails())
            |> Observable.subscribe Authenticate

        loginWindow.Root