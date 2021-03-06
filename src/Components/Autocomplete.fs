namespace Ionide.VSCode.FSharp

open System
open FunScript
open FunScript.TypeScript
open FunScript.TypeScript.vscode
open FunScript.TypeScript.vscode.languages

open DTO
open Ionide.VSCode.Helpers

[<ReflectedDefinition>]
module Autocomplete =
    let private createProvider () =
        let provider = createEmpty<CompletionItemProvider> ()

        let convertToInt code =
            match code with
            | "C" -> 6      (*  CompletionItemKind.Class      *)
            | "E" -> 12     (*  CompletionItemKind.Enum       *)
            | "S" -> 6      (*  CompletionItemKind.Value      *)
            | "I" -> 7      (*  CompletionItemKind.Interface  *)
            | "N" -> 8      (*  CompletionItemKind.Module     *)
            | "M" -> 1      (*  CompletionItemKind.Method     *)
            | "P" -> 9      (*  CompletionItemKind.Property   *)
            | "F" -> 4      (*  CompletionItemKind.Field      *)
            | "T" -> 6      (*  CompletionItemKind.Class      *)
            | _   -> 0

        let mapCompletion (doc : TextDocument) (pos : Position) (o : CompletionResult) =
            o.Data |> Array.map (fun c ->
                let range = doc.getWordRangeAtPosition pos
                let length = if JS.isDefined range then range._end.character - range.start.character else 0.
                let result = createEmpty<CompletionItem> ()
                result.kind <- c.GlyphChar |> convertToInt |> unbox
                result.label <- c.Name
                result.insertText <- c.ReplacementText
                result)

        let mapHelptext (sug : CompletionItem) (o : HelptextResult) =
            let res = (o.Data.Overloads |> Array.fold (fun acc n -> (n |> Array.toList) @ acc ) []).Head
            sug.documentation <- res.Comment
            sug.detail <- res.Signature
            sug

        provider.``provideCompletionItems <-`` (fun doc pos _ ->
            LanguageService.parse doc.fileName (doc.getText ())
            |> Promise.bind (fun _ -> LanguageService.completion (doc.fileName) (int pos.line + 1) (int pos.character + 1))
            |> Promise.success (mapCompletion doc pos)
            |> Promise.toThenable)

        provider.``resolveCompletionItem <-``(fun sug _ ->
            LanguageService.helptext sug.label
            |> Promise.success (mapHelptext sug)
            |> Promise.toThenable)

        provider

    let activate selector (disposables: Disposable[]) =
        Globals.registerCompletionItemProviderOverload2(selector, createProvider(), [|"."|])
        |> ignore
        ()
