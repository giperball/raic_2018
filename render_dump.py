import os
import select
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
local_runner = "codeball2018.exe"

if len(sys.argv) == 1:
    print('Enter token:')
    token = input()
else:
    token = sys.argv[1]

print("Using token: " + token)
# subprocess.Popen([
#     repeater_bat,
#     token
# ],
# shell=True,
# cwd=os.path.dirname(repeater_bat)).wait()

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

build('.', 'render_dump_build')

print('Builded, run now')

my_env = os.environ.copy()
my_env['SAVE_DUMP_NOW'] = 'yes'
subprocess.Popen([
    'dotnet',
    'render_dump_build/ConsoleApp1.dll',
    '127.0.0.1',
    '31001',
    '0000000000000000',
],
    env=my_env)

while True:
    pass

# lr = subprocess.Popen([
#     local_runner,
#     '--seed', '0',
#     '--p2', 'empty',
#     '--p1-name', 'current',
#     '--p2-name', os.path.basename(zip_file),
#     '--noshow',
#     '--log-file', os.path.abspath(regression_logs + '/%s.log' % os.path.basename(zip_file)),
#     '--results-file', os.path.abspath(regression_logs + '/%s.res' % os.path.basename(zip_file)),
# ])
