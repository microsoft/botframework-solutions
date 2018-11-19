ECHO Updating deployment files in Automotive Skill
call ludown parse toluis --in %0\..\..\..\..\assistant\CognitiveModels\LUIS\en\general.lu -o %0\..\en -n General --out general.luis
call ludown parse toluis --in %0\..\..\CognitiveModels\LUIS\en\vehiclesettings.lu -o %0\..\en -n vehiclesettings --out vehiclesettings.luis
call ludown parse toluis --in %0\..\..\CognitiveModels\LUIS\en\vehiclesettings_name_selection.lu -o %0\..\en -n vehiclesettings_name_selection --out vehiclesettings_name_selection.luis
call ludown parse toluis --in %0\..\..\CognitiveModels\LUIS\en\vehiclesettings_value_selection.lu -o %0\..\en -n vehiclesettings_value_selection --out vehiclesettings_value_selection.luis
call ludown parse toluis --in %0\..\..\CognitiveModels\LUIS\en\vehiclesettings_change_confirmation.lu -o %0\..\en -n vehiclesettings_change_confirmation --out vehiclesettings_change_confirmation.luis

