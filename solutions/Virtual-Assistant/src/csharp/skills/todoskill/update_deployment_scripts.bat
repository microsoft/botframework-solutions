ECHO Updating deployment files in ToDo Skill
call ludown parse toluis --in ../../assistant/CognitiveModels/LUIS/en/general.lu -o DeploymentScripts/en -n General --out general.luis
call ludown parse toluis --in CognitiveModels/LUIS/en/todo.lu -o DeploymentScripts/en -n ToDo --out todo.luis
call ludown parse toluis --in ../../assistant/CognitiveModels/LUIS/de/general.lu -o DeploymentScripts/de -n General --out general.luis
call ludown parse toluis --in CognitiveModels/LUIS/de/todo.lu -o DeploymentScripts/de -n ToDo --out todo.luis
call ludown parse toluis --in ../../assistant/CognitiveModels/LUIS/es/general.lu -o DeploymentScripts/es -n General --out general.luis
call ludown parse toluis --in CognitiveModels/LUIS/es/todo.lu -o DeploymentScripts/es -n ToDo --out todo.luis
call ludown parse toluis --in ../../assistant/CognitiveModels/LUIS/fr/general.lu -o DeploymentScripts/fr -n General --out general.luis
call ludown parse toluis --in CognitiveModels/LUIS/fr/todo.lu -o DeploymentScripts/fr -n ToDo --out todo.luis
call ludown parse toluis --in ../../assistant/CognitiveModels/LUIS/it/general.lu -o DeploymentScripts/it -n General --out general.luis
call ludown parse toluis --in CognitiveModels/LUIS/it/todo.lu -o DeploymentScripts/it -n ToDo --out todo.luis
call ludown parse toluis --in ../../assistant/CognitiveModels/LUIS/zh/general.lu -o DeploymentScripts/zh -n General --out general.luis
call ludown parse toluis --in CognitiveModels/LUIS/zh/todo.lu -o DeploymentScripts/zh -n ToDo --out todo.luis