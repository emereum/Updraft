module PublicIp

open System
open System.Net
open System.Text.RegularExpressions
open System.IO

let downloadString (url: string) =
    use client = new WebClient()
    try
        Some (client.DownloadString(url).Trim())
    with
    | :? WebException -> None // todo: logging

let isIpAddress maybeIp =
    Regex.IsMatch(maybeIp, "[0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3}")

let getCurrent (config: Config.Config) =
    match downloadString(config.DynamicIpCheckUrl) with
    | Some maybeIp when not (isIpAddress maybeIp) ->
        raise (ApplicationException("Expected a response that looks like an IP address"))
    | Some ip -> Some ip
    | None -> None

let getLast =
    try
        Some (File.ReadAllText(".last-ip"))
    with
    | :? FileNotFoundException -> None

let setLast last =
    File.WriteAllText (".last-ip", last)
