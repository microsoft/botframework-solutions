# Bot Builder Enterprise Middleware Generator

## Generate middleware

- Open a terminal. 
    - (Optional) In the desired folder for generating the middleware.
- Run the following command for generating your new component.

```bash
> yo botbuilder-enterprise:middleware
```
**At this point you have two different options to procedure**

### Generate middleware using prompts

- The generator will start prompting for some information that is needed for generating the middleware:
    - `What's the name of your middleware? (custom)`
        > The name of your middleware (used also as the root folder's name).
    - `Do you want to change the location of the generation?`
        > A confirmation to change the destination for the generation.
        - `Where do you want to generate the middleware? (by default takes the path where you are running the generator)`
            > The destination path for the generation.
    - `Looking good. Shall I go ahead and create your new middleware?`
        > Final confirmation for creating the desired middleware.

### Generate the middleware using CLI parameters


| Option                                 | Description                                                                                                  |
|----------------------------------------|--------------------------------------------------------------------------------------------------------------|
| -n, --middlewareName <name>            | name of new middleware (by default takes `custom`)                                                           |
| -p, --middlewareGenerationPath <path>  | destination path for the new middleware (by default takes the path where you are runnning the generator)     |
| --noPrompt                             | indicates to avoid the prompts                                                                               |

**NOTE:** If you don't use the _--noPrompt_ option, the process will keep prompting, but using the input values by default.

#### Example

```bash
> yo botbuilder-enterprise:middleware -n newmiddleware -p "\aPath" --noPrompt
```

After this, you can check the summary in your screen:

```bash
- Folder: <amiddlewareFolder>
- Middleware file: <amiddlewareFile> (with .ts extension)
- Path: <aPath>
```

## How to use it
After using the generator, you can add the middleware with the next steps

> NOTE: The following examples are based on a bot created using the Enterprise Bot Template.

### Adding Middleware file

To link your middleware into a bot 

```typescript
export class aBot {
    private readonly MIDDLEWARES: MiddlewareName;
    
    constructor(middlewareName: MiddlewareName) {
        this.MIDDLEWARES = middlewareName;
    }
    
    public async onTurn(Context: TurnContext, next: () => Promise<void>): Promise<void>{
     
        await next();
    }
}
```

## License

MIT © [Microsoft](http://dev.botframework.com)