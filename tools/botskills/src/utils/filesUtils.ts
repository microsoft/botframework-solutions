/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import fs = require('fs');
import { existsSync } from 'fs';

export async function deleteFiles(files: string[]): Promise<void> {
    for(const file of files){
        if(existsSync(file)){
            fs.unlinkSync(file);
        }        
    }
}
