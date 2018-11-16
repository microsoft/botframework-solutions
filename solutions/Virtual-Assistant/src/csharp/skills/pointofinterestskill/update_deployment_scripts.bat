ECHO Updating deployment files in PointOfInterest Skill
call ludown parse toluis --in ../../assistant/CognitiveModels/LUIS/en/general.lu -o DeploymentScripts/en -n General --out general.luis
call ludown parse toluis --in CognitiveModels/LUIS/en/pointofinterest.lu -o DeploymentScripts/en -n PointOfInterest --out pointofinterest.luis
call ludown parse toluis --in ../../assistant/CognitiveModels/LUIS/de/general.lu -o DeploymentScripts/de -n General --out general.luis
call ludown parse toluis --in CognitiveModels/LUIS/de/pointofinterest.lu -o DeploymentScripts/de -n PointOfInterest --out pointofinterest.luis
call ludown parse toluis --in ../../assistant/CognitiveModels/LUIS/es/general.lu -o DeploymentScripts/es -n General --out general.luis
call ludown parse toluis --in CognitiveModels/LUIS/es/pointofinterest.lu -o DeploymentScripts/es -n PointOfInterest --out pointofinterest.luis
call ludown parse toluis --in ../../assistant/CognitiveModels/LUIS/fr/general.lu -o DeploymentScripts/fr -n General --out general.luis
call ludown parse toluis --in CognitiveModels/LUIS/fr/pointofinterest.lu -o DeploymentScripts/fr -n PointOfInterest --out pointofinterest.luis
call ludown parse toluis --in ../../assistant/CognitiveModels/LUIS/it/general.lu -o DeploymentScripts/it -n General --out general.luis
call ludown parse toluis --in CognitiveModels/LUIS/it/pointofinterest.lu -o DeploymentScripts/it -n PointOfInterest --out pointofinterest.luis
call ludown parse toluis --in ../../assistant/CognitiveModels/LUIS/zh/general.lu -o DeploymentScripts/zh -n General --out general.luis
call ludown parse toluis --in CognitiveModels/LUIS/zh/pointofinterest.lu -o DeploymentScripts/zh -n PointOfInterest --out pointofinterest.luis