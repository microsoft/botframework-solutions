/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { CommonUtil } from './commonUtil';

export class ConfigData {
    // tslint:disable:variable-name
    private _maxReadSize: number = CommonUtil.maxReadSize;
    private _maxDisplaySize: number = CommonUtil.maxDisplaySize;
    // tslint:enable:variable-name

    public static instance: ConfigData = new ConfigData();

    constructor() {
        this.maxReadSize = CommonUtil.maxReadSize;
        this.maxDisplaySize = CommonUtil.maxDisplaySize;
    }

    public get maxReadSize(): number {
        return this._maxReadSize;
    }

    public set maxReadSize(value: number) {
        this._maxReadSize = Math.min(value, CommonUtil.maxReadSize);
    }

    public get maxDisplaySize(): number {
        return this._maxDisplaySize;
    }

    public set maxDisplaySize(value: number) {
        this._maxDisplaySize = Math.min(value, CommonUtil.maxDisplaySize);
    }
 }
