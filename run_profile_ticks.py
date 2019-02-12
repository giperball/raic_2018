import os
import subprocess
import sys
import time
from threading import Thread


def build(folder, out_folder=None):
    build_string = "dotnet build -c Release -v q"
    if out_folder:
        build_string = build_string + ' -o ' + out_folder

    print(build_string)

    if subprocess.Popen(build_string.split(' '), cwd=folder, stdout=subprocess.DEVNULL,
                        stderr=subprocess.DEVNULL).wait() != 0:
        raise Exception('Cannot build %s' % folder)


repeater_jar = "repeater.jar"

if len(sys.argv) == 1:
    print('Enter token:')
    token = input()
else:
    token = sys.argv[1]

print("Using token: " + token)

p = subprocess.Popen([
    'java',
    '-jar',
    repeater_jar,
    token
],
    stdout=subprocess.PIPE,
    # shell=True,
    cwd=os.path.dirname(repeater_jar))

downloaded = False


def reader(f, buffer):
    while True:
        line=f.readline()
        if line:
            buffer.append(line)
        else:
            break


linebuffer = []
t = Thread(target=reader, args=(p.stdout, linebuffer))
t.daemon = True
t.start()

while True:
    if linebuffer:
        text = str(linebuffer.pop(0))
        print('TEXT ' + text)
        if 'Got dump ' in text or 'Dump has been downloaded' in text:
            break
    else:
        time.sleep(1)

print('Downloaded')

build('.', 'tick_profile')

print('Builded, run now')

my_env = os.environ.copy()
my_env['TICK_TIME_PROFILE'] = 'yes'
my_env['PRINT_FROM_TICK'] = '5000'
subprocess.Popen([
    'dotnet',
    'tick_profile/ConsoleApp1.dll',
    '127.0.0.1',
    '31001',
    '0000000000000000',
],
    env=my_env)

while True:
    pass
