/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { join } from 'path';

// eslint-disable-next-line @typescript-eslint/no-namespace
export namespace ResponsesUtil {
    export function getResourcePath(resourceName: string, resourcePath: string, locale: string): string {
        let jsonPath: string = join(resourcePath, `${ resourceName }.${ locale }.json`);

        try {
            require.resolve(jsonPath);
        } catch (errLocale) {
            jsonPath = join(resourcePath, `${ resourceName }.json`);

            // Search for the common resource
            try {
                require.resolve(jsonPath);
            } catch (err) {
                throw new Error(`Unable to find '${ resourceName }' in '${ resourcePath }'`);
            }
        }

        return jsonPath;
    }
}
      