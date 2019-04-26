import { readdirSync, readFileSync } from 'fs';
import i18next from 'i18next';
import { basename, isAbsolute, join } from 'path';

export namespace Locales {
    const defaultLocalesPath: string = join(__dirname, 'locales');

    export async function addResourcesFromPath(instance: i18next.i18n, namespace: string, localesPath?: string): Promise<void> {
        if (!instance) { throw new Error(('Missing parameter.  \'instance\' is required')); }
        if (!namespace) { throw new Error(('Missing parameter.  \'namespace\' is required')); }
        if (localesPath && !isAbsolute(localesPath)) { throw new Error(('\'localesPath\' must be an absolute path')); }

        const localesDir: string = localesPath || defaultLocalesPath;
        const files: string[] = readdirSync(localesDir)
            .filter((file: string) => file.endsWith('.json'));
        files.forEach((file: string) => {
            const language: string = basename(file, '.json');
            const languagePath: string = join(localesDir, file);
            const resource: Object = JSON.parse(readFileSync(languagePath, 'utf8'));
            instance.addResourceBundle(language, namespace, resource, true, false);
        });
    }
}
