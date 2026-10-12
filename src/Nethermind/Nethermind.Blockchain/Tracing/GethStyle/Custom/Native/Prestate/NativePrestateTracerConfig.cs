// SPDX-FileCopyrightText: 2024 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System.Text.Json;
using Nethermind.Serialization.Json;

namespace Nethermind.Blockchain.Tracing.GethStyle.Custom.Native.Prestate;

public class NativePrestateTracerConfig
{
    public bool DiffMode { get; init; }

    internal static NativePrestateTracerConfig FromJson(JsonElement? json, JsonSerializerOptions options)
    {
        JsonConfig? config = TypeInfoJsonSerializer.Deserialize<JsonConfig>(json, options);
        return new() { DiffMode = config?.DiffMode ?? false };
    }

    // encoding/json leaves scalar booleans unchanged on null, including after a duplicate key.
    internal sealed class JsonConfig
    {
        public bool? DiffMode { get; set => field = value ?? field; } = false;
    }
}
