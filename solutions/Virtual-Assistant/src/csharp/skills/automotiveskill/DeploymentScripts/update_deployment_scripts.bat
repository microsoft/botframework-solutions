ECHO Updating deployment files in Automotive Skill
call ludown parse toluis --in %0\..\..\..\..\assistant\CognitiveModels\LUIS\en\general.lu -o %0\..\en -n General --out general.luis
call ludown parse toluis --in %0\..\..\CognitiveModels\LUIS\en\settings.lu -o %0\..\en -n settings --out settings.luis
call ludown parse toluis --in %0\..\..\CognitiveModels\LUIS\en\settings_name.lu -o %0\..\en -n settings_name --out settings_name.luis
call ludown parse toluis --in %0\..\..\CognitiveModels\LUIS\en\settings_value.lu -o %0\..\en -n settings_value --out settings_value.luis

