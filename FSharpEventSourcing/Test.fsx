#I __SOURCE_DIRECTORY__
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
        let writeEvents file events =
            let data = Json.serialize events
            File.WriteAllText(file, data)

        if not(Directory.Exists(dir)) then
            Directory.CreateDirectory(dir) |> ignore

        let file = Path.Combine(dir, topic + ".json")

        if not(File.Exists(file)) then
            [|e|]
            |> writeEvents file
        else
            let events = read<'a> dir topic
            Array.append events [|e|]
            |> writeEvents file
            

module OrderDomain =

    type OrderLine = {
        Id : int
        ItemId : string
        Amount : decimal
    }

    type Order = {
        Id : Guid
        OrderLines : List<OrderLine>
    }

    type CreateOrder = {
        Id : Guid
    }

    type AddOrderLine = {
        LineId : int
        ItemId : string
        Amount : decimal
    }

    type RemoveOrderLine = {
        LineId : int            
    }

    type Request =
    | Create of CreateOrder
    | AddLine of AddOrderLine
    | RemoveLine of RemoveOrderLine

    module Order =
        let init = {
            Id = Guid.Parse "00000000-0000-0000-0000-000000000000"
            OrderLines = []
        }

        let create (o : Order) (c : CreateOrder)  =
            {o with Id = c.Id}

        let addLine (o : Order) (l : AddOrderLine) =
            let newLine = {
                Id = l.LineId
                ItemId = l.ItemId
                Amount = l.Amount
            }
            let newLines = o.OrderLines @ [newLine]
            {o with OrderLines = newLines}

        let removeLine (o : Order) (r : RemoveOrderLine) =
            let newLines = o.OrderLines |> List.where (fun l -> l.Id <> r.LineId)
            {o with OrderLines = newLines}

    module Request =
        open Order

        let map (o : Order) r  =
            match r with
            | Create c -> create o c
            | AddLine a -> addLine o a
            | RemoveLine r -> removeLine o r







open EventDB

let eventRepoDir = @".\EventRepo"

// open OrderDomain
// let id = Guid.NewGuid()


// let line = {
//     Id = 1
//     ItemId = "a"
//     Amount = 13M
// }

// let order = 
//     Order.create id 
//     |> Order.addLine line

// printfn "%A" order

open OrderDomain
let r1 = Request.Create {
    Id = Guid.NewGuid()
}
let r2 = Request.AddLine {
    LineId = 1
    ItemId = "a"
    Amount = 5M
}
let r3 = Request.AddLine {
    LineId = 1
    ItemId = "b"
    Amount = 6M
}


open OrderDomain

// let order =  List.fold (OrderDomain.Order.Request.map) (Order.init) ([r1; r2; r3])
// printfn "%A" order