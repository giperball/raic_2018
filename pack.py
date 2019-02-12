import os
from subprocess import call
curNumber = 0
for root, directories, filenames in os.walk('packs'):
	curNumber = "%02d" % (len(filenames) + 1)
	
zipName = "packs/" + 'pack_0' + str(curNumber) + '.zip'
files = []

blacklist = [
	'Model',
	'obj',
	'Properties',
	'bin',
	'packs',
	'Tests',
	'render_strat',
]

for root, directories, filenames in os.walk('.'):
	for filename in filenames: 
		if not filename.endswith('.cs'):
			continue
		fullPath = os.path.join(root,filename) 
		
		blacked = False
		for blackItem in blacklist:
			if '\\%s\\' % blackItem in fullPath:
				blacked = True
				break
		
		if not blacked:
			files.append(fullPath)

print(files)		
call(['7z', 'a', '-tzip', zipName] + files)
print(zipName)
input("Press Enter to continue...")
