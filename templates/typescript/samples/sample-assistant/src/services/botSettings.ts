/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { BotSettingsBase } from 'bot-solutions';
import { TokenExchangeConfig } from '../tokenExchange/';

export interface IBotSettings extends BotSettingsBase {
    tokenExchangeConfig: TokenExchangeConfig;
}
