ECHO Updating deployment files in Email Skill
call ludown parse toluis --in %0\..\..\..\..\assistant\CognitiveModels\LUIS\en\general.lu -o %0\..\en -n General --out general.luis
call ludown parse toluis --in %0\..\..\CognitiveModels\LUIS\en\email.lu -o %0\..\en -n Email --out email.luis
call ludown parse toluis --in %0\..\..\..\..\assistant\CognitiveModels\LUIS\de\general.lu -o %0\..\de -n General --out general.luis
call ludown parse toluis --in %0\..\..\CognitiveModels\LUIS\de\email.lu -o %0\..\de -n Email --out email.luis
call ludown parse toluis --in %0\..\..\..\..\assistant\CognitiveModels\LUIS\es\general.lu -o %0\..\es -n General --out general.luis
call ludown parse toluis --in %0\..\..\CognitiveModels\LUIS\es\email.lu -o %0\..\es -n Email --out email.luis
call ludown parse toluis --in %0\..\..\..\..\assistant\CognitiveModels\LUIS\fr\general.lu -o %0\..\fr -n General --out general.luis
call ludown parse toluis --in %0\..\..\CognitiveModels\LUIS\fr\email.lu -o %0\..\fr -n Email --out email.luis
call ludown parse toluis --in %0\..\..\..\..\assistant\CognitiveModels\LUIS\it\general.lu -o %0\..\it -n General --out general.luis
call ludown parse toluis --in %0\..\..\CognitiveModels\LUIS\it\email.lu -o %0\..\it -n Email --out email.luis
call ludown parse toluis --in %0\..\..\..\..\assistant\CognitiveModels\LUIS\zh\general.lu -o %0\..\zh -n General --out general.luis
call ludown parse toluis --in %0\..\..\CognitiveModels\LUIS\zh\email.lu -o %0\..\zh -n Email --out email.luis