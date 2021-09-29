// Fetching libraries from nuget
#r "nuget: Akka, 1.4.25"
#r "nuget: Akka.FSharp, 1.4.25"
#r "nuget: Akka.Remote, 1.4.25"

// Inititalizing Libraries
open Akka
open Akka.FSharp
open Akka.Actor
open System
open System.Text
open System.Security.Cryptography
open Akka.Configuration
open Akka.Remote
open System.Diagnostics


// Creating the routing configuration
let config = 
    ConfigurationFactory.ParseString(
        @"akka {
            actor {
                provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
                debug : {
                    receive : on
                    autoreceive : on
                    lifecycle : on
                    event-stream : on
                    unhandled : on
                }
            }
            remote {
                helios.tcp {
                    transport-protocol = tcp
                    port = 9001
                    hostname = 10.20.31.3
                }
            }
        }")
// Creating an Actor System
let system = System.create "coins" config

// Discriminated Union for needed Data Structs
type DataInfo =
    | Input of (Int32)
    | Final of (string * string)
    | Done of (string)


// Some Initialization
let zeroInByte : byte = byte 0
let charSet = Array.concat([[|'a' .. 'z'|];[|'A' .. 'Z'|];[|'0' .. '9'|]])
let charLength = charSet.Length


// Function to calculate SHA256 Hash of string and return it in byte array 
let HashByteArray (str : string) : byte[] =
    SHA256Managed.Create().ComputeHash(Encoding.ASCII.GetBytes(str))


//  Convert SHA256 Byte array to string form
let HashByteToString (hash_bytes : byte[]) : string = 
    BitConverter.ToString(hash_bytes).Replace("-","")


// Supervising Options
let options = [SpawnOption.SupervisorStrategy(Strategy.OneForOne (fun e -> Directive.Restart))]
// Supervisior Strategy
let strategy = 
    Strategy.OneForOne (fun e ->
        match e with
        | :? DivideByZeroException -> Directive.Resume
        | :? ArgumentException -> Directive.Stop
        | _ -> Directive.Escalate)