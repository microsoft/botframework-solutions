import argparse
import os
import typing
import functools

parser = argparse.ArgumentParser(description='Convert tsv to lu')
parser.add_argument('file',type=str)
parser.add_argument('-s','--source',type=str,default='custom editorial')
parser.add_argument('-o','--out',type=str,default=None)
args = parser.parse_args()

with open(args.file, 'r', encoding='utf-8') as fin:
    # TODO skip first line
    lines = fin.readlines()[1:]

class Questions:
    def __init__(self, source: str, metadata: str):
        self.questions = []
        self.source = source
        if metadata:
            metadata = metadata.split(':')
            self.metadatas = [[metadata[0], metadata[1]]]
        else:
            self.metadatas = []

    def WriteToFile(self, fout: typing.IO, answer: str):
        def writeLine(*args):
            for arg in args:
                fout.write(arg)
            fout.write('\n')
        writeLine('> Source: ', self.source)
        writeLine('## ? ', self.questions[0])
        for i in range(1, len(self.questions)):
            writeLine('- ', self.questions[i])
        writeLine()
        if self.metadatas:
            writeLine('**Filters:**')
            for metadata in self.metadatas:
                writeLine('- {0} = {1}'.format(metadata[0], metadata[1]))
            writeLine()
        writeLine('```markdown')
        writeLine(answer)
        writeLine('```')
        writeLine()

answerToQuestions: typing.Dict[str, Questions] = {}

for line in lines:
    line = line.split('\t')
    question = line[0]
    answer = line[1]
    source = line[2].strip() if len(line) >= 3 else parser.source
    metadata = line[3].strip() if len(line) >= 4 else None
    questions = answerToQuestions.setdefault(answer, Questions(source, metadata))
    questions.questions.append(question)

print('lines {0} answers {1} questions {2}'.format(len(lines), len(answerToQuestions), functools.reduce(lambda a, b: len(a.questions) + len(b.questions) if isinstance(a, Questions) else a + len(b.questions), answerToQuestions.values())))

with open(args.out if args.out else args.file + '.qna', 'w', encoding='utf-8') as fout:
    for k, v in answerToQuestions.items():
        v.WriteToFile(fout, k)