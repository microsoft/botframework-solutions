ECHO Updating deployment files in PointOfInterest Skill
call ludown parse toluis --in %0\..\..\..\..\assistant\CognitiveModels\LUIS\en\general.lu -o %0\..\en -n General --out general.luis
call ludown parse toluis --in %0\..\..\CognitiveModels\LUIS\en\pointofinterest.lu -o %0\..\en -n PointOfInterest --out pointofinterest.luis
call ludown parse toluis --in %0\..\..\..\..\assistant\CognitiveModels\LUIS\de\general.lu -o %0\..\de -n General --out general.luis
call ludown parse toluis --in %0\..\..\CognitiveModels\LUIS\de\pointofinterest.lu -o %0\..\de -n PointOfInterest --out pointofinterest.luis
call ludown parse toluis --in %0\..\..\..\..\assistant\CognitiveModels\LUIS\es\general.lu -o %0\..\es -n General --out general.luis
call ludown parse toluis --in %0\..\..\CognitiveModels\LUIS\es\pointofinterest.lu -o %0\..\es -n PointOfInterest --out pointofinterest.luis
call ludown parse toluis --in %0\..\..\..\..\assistant\CognitiveModels\LUIS\fr\general.lu -o %0\..\fr -n General --out general.luis
call ludown parse toluis --in %0\..\..\CognitiveModels\LUIS\fr\pointofinterest.lu -o %0\..\fr -n PointOfInterest --out pointofinterest.luis
call ludown parse toluis --in %0\..\..\..\..\assistant\CognitiveModels\LUIS\it\general.lu -o %0\..\it -n General --out general.luis
call ludown parse toluis --in %0\..\..\CognitiveModels\LUIS\it\pointofinterest.lu -o %0\..\it -n PointOfInterest --out pointofinterest.luis
call ludown parse toluis --in %0\..\..\..\..\assistant\CognitiveModels\LUIS\zh\general.lu -o %0\..\zh -n General --out general.luis
call ludown parse toluis --in %0\..\..\CognitiveModels\LUIS\zh\pointofinterest.lu -o %0\..\zh -n PointOfInterest --out pointofinterest.luis