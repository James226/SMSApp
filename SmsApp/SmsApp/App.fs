module App

open System
open System.Windows
open System.Windows.Controls
open FSharpx
open SMSApp

[<STAThread>]
(new Application()).Run(Login.LoadWindow()) |> ignore