import { TelemetryClient } from "applicationinsights";
import { DateTimePrompt } from "botbuilder-dialogs";
import { start } from "repl";
import { duration } from "moment";

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License
   
   export class TelemetryBotDependencyExtension
    {
        public static DependencyType: string = "Bot";
     
        /**
         * Send information about a dependency in the Bot Application.
         * TypeParam <TResult> The type of the return value of the method that this delegate encapsulates.
         * The action delegate will be timed and an Application Insights dependency record will be created.
         * @param {TelemetryClient} telemetryClient - The TelemetryClient.
         * @param {TResult} action - Encapsulates a method that has no parameters and returns a value of the type specified by the TResult parameter.
         * @param {string} dependencyName - Name of the command initiated with this dependency call. Low cardinality value. Examples are stored procedure name and URL path template.
         * @param {string} dependencyData - Command initiated by this dependency call. For example, Middleware.
         * @returns The return value of the method that this delegate encapsulates.
         */
          public static trackBotDependency<TResult>(telemetryClient: TelemetryClient, action: () => TResult, dependencyName: string, dependencyData: string): TResult {
          // return action();

           if (dependencyName === null) {
             throw new Error("Missing parameter, dependencyName is required"); 
           }
           var startTime = Date.now();         
           var success = true;

           try {
               return action();
           }
           catch (Exception) {
               success = false;
               throw Exception;
           }
           finally {
               var endTime = Date.now();
               let duration = endTime - startTime;
               // Log the dependency into Application Insights
               telemetryClient.trackDependency({
                   dependencyTypeName: TelemetryBotDependencyExtension.DependencyType,
                   name: dependencyName,
                   data: dependencyData,
                   success: success,
                   duration: duration,
                   resultCode: 0
               });
           }
       }
    }
