﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Solutions.Testing.Mocks;

namespace PhoneSkill.Tests.TestDouble
{
    /// <summary>
    /// Builder for MockLuisRecognizer's.
    /// </summary>
    /// <typeparam name="TLuisResult">The LUIS result type as generated by the LUISGen tool.</typeparam>
    /// <typeparam name="TIntent">The intent type for the LUIS result type. This should be TLuisResult.Intent.</typeparam>
    public class MockLuisRecognizerBuilder<TLuisResult, TIntent>
        where TLuisResult : IRecognizerConvert, new()
    {
        private Dictionary<string, IRecognizerConvert> utterances;

        public MockLuisRecognizerBuilder()
        {
            Reset();
        }

        /// <summary>
        /// Reset the internal state of this builder, so it can be reused to build another MockLuisRecognizer.
        /// </summary>
        /// <returns>This.</returns>
        public MockLuisRecognizerBuilder<TLuisResult, TIntent> Reset()
        {
            utterances = new Dictionary<string, IRecognizerConvert>();
            return this;
        }

        /// <summary>
        /// Finish building the MockLuisRecognizer. This also resets the internal state of this builder, so it can be reused to build another MockLuisRecognizer.
        /// </summary>
        /// <returns>The built MockLuisRecognizer.</returns>
        public MockLuisRecognizer Build()
        {
            var intentType = typeof(TIntent);
            var intentValues = intentType.GetEnumValues();
            var intentNames = intentType.GetEnumNames();
            for (int i = 0; i < intentValues.Length; ++i)
            {
                if ("None".Equals(intentNames[i]))
                {
                    var noneIntent = (TIntent)intentValues.GetValue(i);
                    var recognizer = new MockLuisRecognizer(defaultIntent: CreateIntent(string.Empty, noneIntent));
                    recognizer.RegisterUtterances(utterances);

                    Reset();

                    return recognizer;
                }
            }

            throw new Exception($"Cannot find enum value {intentType.FullName}.None.");
        }

        /// <summary>
        /// Add a canned response for the given utterance to the MockLuisRecognizer.
        /// </summary>
        /// <param name="userInput">The user's query.</param>
        /// <param name="intent">The intent to be returned by the MockLuisRecognizer.</param>
        /// <param name="entities">The entities to be returned by the MockLuisRecognizer.</param>
        /// <returns>This.</returns>
        public MockLuisRecognizerBuilder<TLuisResult, TIntent> AddUtterance(string userInput, TIntent intent, IList<MockLuisEntity> entities = null)
        {
            utterances[userInput] = CreateIntent(userInput, intent, entities);
            return this;
        }

        private static TLuisResult CreateIntent(string userInput, TIntent intent, IList<MockLuisEntity> entities = null)
        {
            var type = typeof(TLuisResult);
            var result = new TLuisResult();
            type.GetField("Text").SetValue(result, userInput);

            var intents = new Dictionary<TIntent, IntentScore>();
            intents.Add(intent, new IntentScore() { Score = 0.9 });
            type.GetField("Intents").SetValue(result, intents);

            var entitiesField = type.GetField("Entities");
            if (entitiesField == null && entities != null)
            {
                throw new Exception($"Cannot access {type.FullName}.Entities.");
            }

            var entitiesType = type.GetNestedType("_Entities");
            var instanceType = entitiesType.GetNestedType("_Instance");
            var entitiesObject = Instantiate(entitiesType);
            var instanceObject = Instantiate(instanceType);

            if (entities != null)
            {
                var typeToEntities = new Dictionary<string, List<MockLuisEntity>>();
                foreach (var entity in entities)
                {
                    if (!typeToEntities.TryGetValue(entity.Type, out var entityList))
                    {
                        entityList = new List<MockLuisEntity>();
                    }

                    entityList.Add(entity);
                    typeToEntities[entity.Type] = entityList;
                }

                foreach (var (entityType, entityList) in typeToEntities)
                {
                    var entityTypeField = entitiesType.GetField(entityType);
                    if (entityTypeField.FieldType.FullName == "System.String[]")
                    {
                        entityTypeField.SetValue(entitiesObject, GetEntityValues(entityList));
                    }
                    else if (entityTypeField.FieldType.FullName == "System.String[][]")
                    {
                        entityTypeField.SetValue(entitiesObject, GetListEntityValues(entityList));
                    }
                    else
                    {
                        throw new Exception($"Cannot set {type.FullName}._Entities.{entityType} because the type {entityTypeField.FieldType.FullName} is not supported.");
                    }

                    var instanceDataArray = new InstanceData[entityList.Count];
                    for (int i = 0; i < entityList.Count; ++i)
                    {
                        var entity = entityList[i];
                        instanceDataArray[i] = new InstanceData
                        {
                            Type = entity.Type,
                            Text = entity.Text,
                            StartIndex = entity.StartIndex,

                            // The end index is inclusive.
                            EndIndex = entity.StartIndex + entity.Text.Length - 1,
                        };
                    }

                    instanceType.GetField(entityType).SetValue(instanceObject, instanceDataArray);
                }
            }

            if (entitiesField != null)
            {
                entitiesType.GetField("_instance").SetValue(entitiesObject, instanceObject);
                entitiesField.SetValue(result, entitiesObject);
            }

            return result;
        }

        private static object Instantiate(Type type)
        {
            return type.GetConstructor(types: new Type[0]).Invoke(parameters: new object[0]);
        }

        private static string[] GetEntityValues(IList<MockLuisEntity> entities)
        {
            var values = new string[entities.Count];
            for (int i = 0; i < entities.Count; ++i)
            {
                values[i] = entities[i].Text;
            }

            return values;
        }

        private static string[][] GetListEntityValues(IList<MockLuisEntity> entities)
        {
            var values = new string[entities.Count][];
            for (int i = 0; i < entities.Count; ++i)
            {
                var value = entities[i].ResolvedValue;
                if (string.IsNullOrEmpty(value))
                {
                    value = entities[i].Text;
                }

                values[i] = new string[1] { value };
            }

            return values;
        }
    }
}
