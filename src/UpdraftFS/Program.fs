open UpdraftService
open Amazon.EC2
open System
open Amazon

// Learn more about F# at http://fsharp.org
// See the 'F# Tutorial' project for more help.

[<EntryPoint>]
let main argv = 
    printfn "%A" argv

    UpdraftService.doUpdate() 

    0 // return an integer exit code
