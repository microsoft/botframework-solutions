import { ITelemetryLuisRecognizer, ITelemetryQnAMaker } from '../middleware';

export class LocaleConfiguration {

    public locale!: string;

    public dispatchRecognizer!: ITelemetryLuisRecognizer;

    public luisServices: Map<string, ITelemetryLuisRecognizer> = new Map();

    public qnaServices: Map<string, ITelemetryQnAMaker> = new Map();
}
