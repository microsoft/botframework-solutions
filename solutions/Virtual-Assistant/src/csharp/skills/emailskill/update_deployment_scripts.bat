ECHO Updating deployment files in Email Skill
call ludown parse toluis --in ../../assistant/CognitiveModels/LUIS/en/general.lu -o DeploymentScripts/en -n General --out general.luis
call ludown parse toluis --in CognitiveModels/LUIS/en/email.lu -o DeploymentScripts/en -n Email --out email.luis
call ludown parse toluis --in ../../assistant/CognitiveModels/LUIS/de/general.lu -o DeploymentScripts/de -n General --out general.luis
call ludown parse toluis --in CognitiveModels/LUIS/de/email.lu -o DeploymentScripts/de -n Email --out email.luis
call ludown parse toluis --in ../../assistant/CognitiveModels/LUIS/es/general.lu -o DeploymentScripts/es -n General --out general.luis
call ludown parse toluis --in CognitiveModels/LUIS/es/email.lu -o DeploymentScripts/es -n Email --out email.luis
call ludown parse toluis --in ../../assistant/CognitiveModels/LUIS/fr/general.lu -o DeploymentScripts/fr -n General --out general.luis
call ludown parse toluis --in CognitiveModels/LUIS/fr/email.lu -o DeploymentScripts/fr -n Email --out email.luis
call ludown parse toluis --in ../../assistant/CognitiveModels/LUIS/it/general.lu -o DeploymentScripts/it -n General --out general.luis
call ludown parse toluis --in CognitiveModels/LUIS/it/email.lu -o DeploymentScripts/it -n Email --out email.luis
call ludown parse toluis --in ../../assistant/CognitiveModels/LUIS/zh-hans/general.lu -o DeploymentScripts/zh-hans -n General --out general.luis
call ludown parse toluis --in CognitiveModels/LUIS/zh-hans/email.lu -o DeploymentScripts/zh-hans -n Email --out email.luis