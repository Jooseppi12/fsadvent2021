namespace fsadvent

open WebSharper
open WebSharper.JavaScript
open WebSharper.UI
open WebSharper.UI.Html
open WebSharper.Sitelets
open WebSharper.UI.Templating
open WebSharper.UI.Client
open WebSharper.Forms

[<JavaScript>]
module Client =
    type EndPoint =
        | [<EndPoint "/">] Home
        | [<EndPoint "/checkout">] Checkout

    let router = Router.Infer<EndPoint>()
    let routerInstance = Router.Install EndPoint.Home router 

    // The templates are loaded from the DOM, so you just can edit index.html
    // and refresh your browser, no need to recompile unless you add or remove holes.
    type IndexTemplate = Template<"wwwroot/index.html", ClientLoad.FromDocument>

    let itemsToOrder : Var<Set<string>> = Var.Create Set.empty

    let items =
        [
            "apple"
            "orange"
            "banana"
            "grape"
            "pear"
        ]

    let cartForm =
        Form.YieldVar itemsToOrder
        |> Form.WithSubmit
    
    let checkoutForm =
        cartForm
        |> Form.Run (fun items ->
            JS.Alert
                <| sprintf "You have ordered: %s" (items |> String.concat ",")
        )
        |> Form.Render (fun itemStore submitter ->
            itemStore.View
            |> Doc.BindView(fun x ->
                x
                |> Set.toList
                |> List.map(fun item ->
                    div [] [
                        span [] [text item]
                        button [
                            on.click (fun _ _ ->
                                itemStore.Update (fun items ->
                                    Set.remove item items
                                )
                            )
                        ] [text "Remove"] 
                    ]
                )
                |> Doc.Concat
            )
            |> fun doc ->
                div [] [
                    h1 [] [text "Checkout Page"]
                    doc
                    div [attr.``class`` "checkout-control"] [
                        button [on.click (fun _ _ -> submitter.Trigger())] [text "Order"]
                        button [on.click (fun _ _ ->
                            routerInstance.Set EndPoint.Home
                        )] [text "Go back"]
                    ]
                ]
        )

    let renderedcart =
        cartForm.Render(fun itemStore _ ->
            itemStore.View
            |> Doc.BindView(fun items ->
                items
                |> Set.toList
                |> List.map (fun item ->
                    div [attr.``class`` "item"] [text item]
                )
                |> Doc.Concat
            )
        )

    let HomeView () =
        IndexTemplate.Home()
            .ItemsToShopFor(
                items
                |> List.map (fun item ->
                    IndexTemplate.Item()
                        .Name(item)
                        .AddToCart(fun _ ->
                            itemsToOrder.Update(fun items -> Set.add item items)
                        )
                        .CanBeAdded(
                            attr.disabledDynPred (View.Const "disabled")
                                (itemsToOrder.View.Map(fun cart -> Seq.contains item cart ))
                        )
                        .Doc()
                )
            )
            .ItemsToCheckout(
                renderedcart
            )
            .GoToCheckout(fun _ -> 
                routerInstance.Set EndPoint.Checkout
            )
            .Doc()
    
    let CheckoutView () =
        checkoutForm

    [<SPAEntryPoint>]
    let Main () =
        routerInstance.View
        |> Doc.BindView (function
            | Home -> HomeView ()
            | Checkout -> CheckoutView ()
        )
        |> Doc.RunById "main"
