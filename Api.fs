module Api

open Freya.Core
open Freya.Machines.Http
open Freya.Types.Http
open Freya.Routers.Uri.Template

open System.Data.SqlClient
open Dapper
open Npgsql
open Chiron


let connString = "Host=localhost;Database=diesel_demo;Username=postgres;Password=example"
let connection = new NpgsqlConnection(connString)
// let connection = new SqlConnection(connString)
connection.Open()

type Post = { 
    Id:int; 
    Title:string; 
    Body:string
    Published:bool }

let dapperQuery<'Result> (query:string) (connection:NpgsqlConnection) =
        connection.Query<'Result>(query)
    
let dapperParametrizedQuery<'Result> (query:string) (param:obj) (connection:SqlConnection) : 'Result seq =
    connection.Query<'Result>(query, param)

let getPosts connection =
    connection
    |> dapperQuery<Post> "SELECT * From posts"

let name_ = Route.atom_ "name"
let salutation_ = Route.atom_ "salutation"

let name =
    freya {
        let! nameO = Freya.Optic.get name_

        match nameO with
        | Some name -> return name
        | None -> return "World" }

let salutation =
    freya {
        let! salutationO = Freya.Optic.get salutation_

        match salutationO with
        | Some salutation -> return salutation
        | None -> return "cruel world!" }

let serializePost (x:Post) = 
    json {
        do! Json.write "id" x.Id
        do! Json.write "title" x.Title
        do! Json.write "body" x.Body
        do! Json.write "published" x.Published
    }
let getSerializedPost post = 
    post 
    |> Json.serializeWith serializePost 
    |> Json.formatWith JsonFormattingOptions.Pretty
let sayHello =
    freya {
        let! name = name
        // let result = getPosts connection
        let firstPost = getPosts connection |> Seq.head
        // let formatCompact = Json.format (Json.serialize result)
        let formatted = getSerializedPost(firstPost)
        printfn "%A" formatted

        return Represent.text (sprintf "%s" formatted) }

let sayGoodbye =
    freya {
        let! salutation = salutation

        return Represent.text (sprintf "Goodbye, %s!" salutation) }

let helloMachine =
    freyaMachine {
        methods [GET; HEAD; OPTIONS]
        handleOk sayHello }

let goodbyeMachine =
    freyaMachine {
        methods [GET; HEAD; OPTIONS]
        handleOk sayGoodbye }

let root =
    freyaRouter {
        resource "/hello{/name}" helloMachine
        resource "/goodbye{/salutation}" goodbyeMachine }
