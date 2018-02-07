#I __SOURCE_DIRECTORY__
open System
open System.Data.SqlTypes
open System.Windows.Forms
#I "../packages/Newtonsoft.Json/lib/net45"
#r "Newtonsoft.Json.dll"

open System


module Json = 
    open Newtonsoft.Json 
    let serialize obj = 
        JsonConvert.SerializeObject(obj, Formatting.Indented)
    let deserialize<'a> str = 
        JsonConvert.DeserializeObject<'a> str

module EventDB =
    open System.IO

    type Event<'a> = {
        Id : Guid
        Timestamp : DateTimeOffset
        Content : 'a
    }

    let read<'a> dir topic : array<Event<'a>>=
        let file = Path.Combine(dir, topic + ".json")
        let text = File.ReadAllText(file)
        Json.deserialize<array<Event<'a>>>(text)

    let readAfter<'a> dir topic (i : int) =
        (read<'a> dir topic).[i..]
        
    let write<'a> dir topic e =
        if not(Directory.Exists(dir)) then
            Directory.CreateDirectory(dir) |> ignore

        let file = Path.Combine(dir, topic + ".json")

        if not(File.Exists(file)) then
            let events = [|e|]
            let data = Json.serialize events
            File.WriteAllText(file, data)
        else
            let events = read<'a> dir topic
            let events = Array.append events [|e|]
            let data = Json.serialize events
            File.WriteAllText(file, data)


open EventDB

let eventRepoDir = @".\EventReop"
type Chicken = {
    Id : Guid
    Name : string
}

type Dog = {
    Id : Guid
    Name : string
    Size : decimal
}

type Duck = {
    Id : Guid
    Name : string
    Weight : decimal
}

let myChicken = {
    Id = Guid.NewGuid()
    Name = "Cluckers"
}

type EventOption =
| Chicken of Chicken
| Dog of Dog
| Duck of Duck

let myDuck = {
    Id = Guid.NewGuid()
    Name = "Mallard"
    Weight = 11.0M
}

let myDog = {
    Id = Guid.NewGuid()
    Name = "Buddy"
    Size = 13.0M
}

let testEvent = {
    Id = Guid.NewGuid()
    Timestamp = DateTimeOffset.UtcNow
    Content = EventOption.Dog myDog
}

EventDB.write eventRepoDir "request" testEvent

let sillyEvents = EventDB.read<EventOption> eventRepoDir "request"
printf "%A" sillyEvents