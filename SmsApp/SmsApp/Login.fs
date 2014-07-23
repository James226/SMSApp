namespace SMSApp

open System
open System.Windows
open System.Windows.Controls
open FSharpx
open FSharp.Data
open FSharp.Data.HttpRequestHeaders

open SmsApp.Models
open SmsApp.ViewModels

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
