﻿using System;
using Eto.Parse.Ast;

namespace Eto.Parse.Samples.Json.Ast
{
	public class JsonAstBuilder : AstBuilder<JsonToken>
	{
		public JsonAstBuilder()
		{
			var token = new CreateBuilder<JsonToken>();

			var jobject = CreatedBy("object", () => new JsonObject());
			jobject.Children().HasKeyValue<JsonObject, string, JsonToken>(
				new ValueBuilder { Name = "name" },
				token
			);

			var jarray = CreatedBy("array", () => new JsonArray());
			jarray.Children().HasMany<JsonArray, JsonToken>().Builders.Add(token);

			token.CreatedBy("string", () => new JsonValue()).Property<string>((o, v) => o.Value = v);
			token.Builders.Add(jobject);
			token.Builders.Add(jarray);
			token.CreatedBy("number", () => new JsonValue()).Property<decimal>((o, v) => o.Value = v);
			token.CreatedBy("bool", () => new JsonValue()).Property<bool>((o, v) => o.Value = v);
			token.CreatedBy(() => (JsonToken)null);
		}
	}
}

