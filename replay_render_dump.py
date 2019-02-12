import os
import subprocess

local_runner = "codeball2018.exe"


def build(folder, out_folder=None):
    build_string = "dotnet build -c Release -v q"
    if out_folder:
        build_string = build_string + ' -o ' + out_folder

    print(build_string)

    if subprocess.Popen(build_string.split(' '), cwd=folder).wait() != 0:
        raise Exception('Cannot build %s' % folder)

build('render_strat')

port = 40404

lr = subprocess.Popen([
    local_runner,
    '--seed', '0',
    '--p1', 'tcp-{}'.format(port),
    '--p1-name', 'replay',
    '--p2', 'empty',
    '--no-countdown',
])

my_env = os.environ.copy()
my_env['RENDER_DUMP_FILE'] = os.path.abspath("render_dump.txt")

subprocess.Popen([
    'dotnet',
    'render_strat/bin/Release/netcoreapp2.1/render_strat.dll',
    '127.0.0.1',
    str(port),
    '0000000000000000',
    ],
    env=my_env)

lr.wait()
