/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { SentimentType } from '../models';

// eslint-disable-next-line @typescript-eslint/no-namespace
export namespace LuisRecognizerEx {
    export const sentiment = 'sentiment';
    export const positiveSentiment = 'positive';
    export const neutralSentiment = 'neutral';
    export const negativeSentiment = 'negative';

    export function getSentimentInfo<T>(luisConverter: T, propertyAccessor: (luisConverter: T) => Map<string, Object>): [SentimentType, number] {
        let sentimentLabel: SentimentType = SentimentType.None;
        let maxScore = 0.0;

        const luisProperty: Map<string, Object> = propertyAccessor(luisConverter);
        const result: Object | undefined = luisProperty.get(sentiment);

        if(luisProperty !== undefined && result !== undefined) {
            let sentimentInfo: any = JSON.parse(result.toString());
            sentimentLabel = getSentimentType(sentimentInfo.label);
            maxScore = sentimentInfo.score !== undefined ? sentimentInfo.score : 0.0;
        }

        return ([sentimentLabel, maxScore]);
    }

   
    export function getSentimentType(label: string): SentimentType {
        let sentimentType: SentimentType = SentimentType.None;

        if (label === positiveSentiment) {
            sentimentType = SentimentType.Positive;
        } else if (label === neutralSentiment) {
            sentimentType = SentimentType.Neutral;
        } else if (label === negativeSentiment) {
            sentimentType = SentimentType.Negative;
        }

        return sentimentType;
    }
}