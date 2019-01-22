import { TelemetryClient } from "applicationinsights";
import { DateTimePrompt } from "botbuilder-dialogs";
import { start } from "repl";
import { duration } from "moment";

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License
   
   export class TelemetryBotDependencyExtension
    {
        public static DependencyType: string = "Bot";
     
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
