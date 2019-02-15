import { TelemetryClient } from 'applicationinsights';

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License

export namespace TelemetryBotDependencyExtension {

    const dependencyType: string = 'Bot';

    /**
     * Send information about a dependency in the Bot Application.
     * TypeParam <TResult> The type of the return value of the method that this delegate encapsulates.
     * The action delegate will be timed and an Application Insights dependency record will be created.
     * @param telemetryClient - The TelemetryClient.
     * @param action - Encapsulates a method that has no parameters and returns a value of the type specified by the TResult parameter.
     * @param dependencyName
     * - Name of the command initiated with this dependency call.
     *   Low cardinality value.
     *   Examples are stored procedure name and URL path template.
     * @param dependencyData - Command initiated by this dependency call. For example, Middleware.
     * @returns The return value of the method that this delegate encapsulates.
     */
    export function trackBotDependency<TResult>(
            telemetryClient: TelemetryClient,
            action: () => TResult,
            dependencyName: string,
            dependencyData: string): TResult {

        if (dependencyName === null) {
            throw new Error('Missing parameter, dependencyName is required');
        }
        const startTime: number = Date.now();
        let success: boolean = true;

        try {
            return action();
        } catch (err) {
            success = false;
            throw err;
        }
        finally {
            const endTime: number = Date.now();
            const duration: number = endTime - startTime;
            // Log the dependency into Application Insights
            telemetryClient.trackDependency({
                dependencyTypeName: dependencyType,
                name: dependencyName,
                data: dependencyData,
                success: success,
                duration: duration,
                resultCode: 0
            });
        }
    }
}
