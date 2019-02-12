import datetime
import os
import subprocess
import time
from shutil import copyfile

old_log_file = 'optimize_test/old.log'
new_log_file = 'optimize_test/new.log'
time_log_file = 'optimize_test/time'

ticks_duration = 4000
port = 32001

first_run = not os.path.exists(old_log_file)

local_runner = "codeball2018.exe"


def build(folder):
    if subprocess.Popen("dotnet build -c Release -v q".split(' '), cwd=folder, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL).wait() != 0:
        raise Exception('Cannot build %s' % folder)


build(".")


if first_run:
    new_log_file = old_log_file

print('Starting strat')

lr = subprocess.Popen([
    local_runner,
    '--seed', '0',
    '--p1', 'tcp-{}'.format(port),
    '--p1-name', 'current',
    '--noshow',
    '--log-file', os.path.abspath(new_log_file),
    '--duration', str(ticks_duration),
])

start = time.time()

subprocess.Popen([
    'dotnet',
    '%s/bin/Release/netcoreapp2.1/ConsoleApp1.dll' % os.path.abspath('.'),
    '127.0.0.1',
    str(port),
    '0000000000000000',
    ])

lr.wait()

end = time.time()

new_time = int(round((end - start) * 1000))

t = datetime.datetime.now().strftime("%Y-%m-%d_%H-%M-%S")

if not first_run:
    with open(time_log_file, 'r') as content_file:
        old_time = int(content_file.read())
    with open(old_log_file, 'r') as content_file:
        old_log = content_file.read()
    with open(new_log_file, 'r') as content_file:
        new_log = content_file.read()

    if old_log != new_log:
        print('')
        print('ERROR!!!!!!!!!!!!!!!!!!! Strategy changed')
        print('')
        copyfile(old_log_file,
                 'optimize_test/' + t + 'old.log')
        copyfile(old_log_file,
                 'optimize_test/old_broken.log')
        copyfile(new_log_file,
                 'optimize_test/' + t + 'new.log')
        copyfile(new_log_file, old_log_file)
    else:
        print('Strategy ok')

    print('Old time {}, new time {}, time diff {}, percent change {}'.format(
        old_time,
        new_time,
        old_time - new_time,
        float(old_time - new_time) / float(old_time) * 100,
    ))
else:
    print('First run recorded')

with open(time_log_file, 'w') as content_file:
    content_file.write(str(new_time))

with open('optimize_test/' + t + '.time', 'w') as content_file:
    content_file.write(str(new_time))

input("Press Enter to continue...")