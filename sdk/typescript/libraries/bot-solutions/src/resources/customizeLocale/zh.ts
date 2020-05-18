/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */
import * as dayjs from 'dayjs';

const locale: {
    name: string;
    weekdays: string[];
    months: string[];
} = {
    name: 'zh',
    weekdays: '星期天_星期一_星期二_星期三_星期四_星期五_星期六'.split('_'),
    months: '一月_二月_三月_四月_五月_六月_七月_八月_九月_十月_十一月_十二月'.split('_')
};

dayjs.locale(locale, undefined, true);
