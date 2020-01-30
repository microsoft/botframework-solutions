# Inline Trigger Utterances functionality
We introduced the possibility to use the utterances declared in the manifest for the connection of a Skill to a Virtual Assistant using Botskills CLI Tool.

This functionality is implemented in the `connect` and `update` commands, and by default the tool looks for the utterances of the `.lu` files declared in the `utteranceSources`.

Adding the `--inlineUtterances` argument in the commands tells to the tool to use the utterances declared in the manifest.

## Examples
```bash
botskills connect --remoteManifest "http://<YOUR_SKILL_MANIFEST>.azurewebsites.net/api/skill/manifest" --inlineUtterances --cs --verbose
```

```bash
botskills update --remoteManifest "http://<YOUR_SKILL_MANIFEST>.azurewebsites.net/api/skill/manifest" --inlineUtterances --cs --verbose
```

### Further Reading
- [Connect Command](./connect.md)