// Fethcing required libraries
#r "nuget: Akka.FSharp" 
#r "nuget: Akka.TestKit"
#r "nuget: Akka.Remote"

// Initializing Libraries
open Akka
open Akka.FSharp
open Akka.Actor
open System
open System.Text
open System.Security.Cryptography
open Akka.Configuration
open Akka.Remote


//  Discriminated Union for needed Data Structs
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


// Client Configuration
let config = 
    ConfigurationFactory.ParseString(
        @"akka {
            actor {
                provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
                
            }
            remote {
                helios.tcp {
                    transport-protocol = tcp
                    port = 0
                    hostname = localhost
                }
            }
        }")


// Getting command line Input for Server IP
let remoteSystemAddress = "akka.tcp://coins@" + Environment.GetCommandLineArgs().[2] + ":9001"

// Creating a function to deploy remote printer
let remoteDeploy systemPath = 
    let address = 
        match ActorPath.TryParseAddress systemPath with
        | true, a -> a
        | false, _ -> failwith "Address is not accepted"
    Deploy(RemoteScope(address))


// Creating actor system
let system = System.create "client" config


// Spawing an actor remotely to the server for printing
let remoteClientPrinter = 
    spawne system "remotePrinter"
        <@ 
            fun mailbox ->
            let rec messageLoop () : Cont<string * string, obj> =
                actor {
                    let! msg = mailbox.Receive()
                    match msg with
                    | (hashIn, hashOut) ->
                        printfn "%s\t%s\n" hashIn hashOut
                        mailbox.Sender() <! "Done Printing"
                    | _ -> logErrorf mailbox "unexpected message: %A" msg
                    return! messageLoop()
                }
            messageLoop()
        @> [SpawnOption.Deploy(remoteDeploy remoteSystemAddress)]
 

 // Supervising Options
let options = [SpawnOption.SupervisorStrategy(Strategy.OneForOne (fun e -> Directive.Restart))]
// Supervisior Strategy
let strategy = 
    Strategy.OneForOne (fun e ->
        match e with
        | :? DivideByZeroException -> Directive.Resume
        | :? ArgumentException -> Directive.Stop
        | _ -> Directive.Escalate)


 // Client Worker Actor
let clientWorkerActor (mailbox : Actor<_>) = 
    let rec messageLoop () = actor {

        // Reading the message
        let! msg = mailbox.Receive()

        
        for i in 1 .. 500 do
            // Random Fucntion
            let randomGenerator = Random()

            // Generating n random strings random strings
            let str = "prateekgoel;" + String(Array.init (randomGenerator.Next(1,500)) (fun _ -> charSet.[randomGenerator.Next(charLength)]))

            // Creating a hash out of it
            let shahashbytearray : byte[] = HashByteArray str

            // Checking for leading zeros
            match msg with
            | Input(userInput) ->
                // let strfound = CheckLeadingZeros shahashbytearray userInput
                let finalHash : string = HashByteToString shahashbytearray
                let leadingString : string = String.replicate userInput "0"
                let strfound = finalHash.StartsWith(leadingString)
                if strfound then
                    async {
                        let! remoteMsg = remoteClientPrinter <? (str, finalHash)
                        match remoteMsg with
                        | _ -> ()
                    }
                    |> Async.RunSynchronously
                else
                    mailbox.Self <! Input(userInput)
                // mailbox.Sender() <! Done("Work Done")
            | _ -> ()      
        return! messageLoop()
    }
    messageLoop ()


// The Boss is here
let clientBossActor (mailbox : Actor<_>) = 
    
    // Checking Processor Cores in the System
    let clientprocessorcount = int <| Environment.ProcessorCount

    // Making many acting to spawn later
    let totalclientworkers = clientprocessorcount * 10

    // Creating a worker Pool
    let clientWorkerPool = 
        [1 .. totalclientworkers]
        |> List.map(fun i -> spawnOpt system (sprintf "ClientWorker_%d" i) clientWorkerActor <| options)

    // Creating a iterable clientworker enumerator
    let clientworkerenum = Array.ofList(clientWorkerPool)

    // Creating a Coordinator to assign the work
    let clientWorkerCoordinator = system.ActorOf(Props.Empty.WithRouter(Routing.RoundRobinGroup(clientworkerenum)))

    // Tracking Worker's State
    let state = ref 0

    let rec messageLoop () = actor {
        let! msg = mailbox.Receive()

        match msg with
        | Input(userInput) ->
            for i in 1 .. totalclientworkers do
                clientWorkerCoordinator <! Input(userInput)
        | Done(workerState) ->
            state := !state + 1
            if !state = totalclientworkers then
                mailbox.Context.System.Terminate() |> ignore
        | _ -> ()

        return! messageLoop()
    }
    messageLoop()
// Spawning the Boss Actor System
let clientBoss = spawn system "clientboss" clientBossActor


// Get user Input for leading zeros
let getServerInput (mailbox : Actor<_>) = 
    let rec messageLoop() = 
        actor {
            let! msg = mailbox.Receive()
            // Invoke Client Boss
            match msg with
            | m ->
                clientBoss <! Input(m)
            | _ -> printfn "Error Encountered"
            return! messageLoop()
        }
    messageLoop()
let getInput = spawn system "serveInput" getServerInput


// Server Reference and Async to get server input
let serverInputAddress : string = remoteSystemAddress + "/user/inputserver"
let serverRef = select serverInputAddress system
async {
    let! msg = serverRef <? "Register"
    match msg with
    | m ->
        getInput <! m
    | _ -> printfn "Error Encountered"
    
} |> Async.RunSynchronously


system.WhenTerminated.Wait()
// Console.ReadLine()
