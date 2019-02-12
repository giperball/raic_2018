import datetime
import os
import subprocess
from multiprocessing.pool import ThreadPool
from os import listdir
from os.path import isfile
import tempfile
import shutil
from distutils.dir_util import copy_tree
from threading import Thread

import threading
threadLock = threading.Lock()
port = 31000
mode = 1

def parse_results(files):
    looses = 0
    for file in files:
        with open(file, 'r') as f:
            current_strat_player = f.readline()
            old_strat = f.readline()
            if current_strat_player.split(':')[2].strip() != 'ok':
                print('New strategy dead at {0}'.format(file))

            if int(old_strat.split(':')[0]) == 2:
                print('Win {0} with count {1}'.format(file, current_strat_player.split(':')[1]) + ':' + old_strat.split(':')[1])
                continue

            looses += 1

            print('New strategy loosed for {0} with count {1}'.format(file, current_strat_player.split(':')[1]) + ':' + old_strat.split(':')[1])

    if looses == 0:
        print('All good')
    else:
        print('Total looses {0}'.format(looses))


def build(folder, out_folder= None):
    build_string = "dotnet build -c Release -v q"
    if out_folder:
        build_string = build_string + ' -o ' + out_folder

    print(build_string)

    if subprocess.Popen(build_string.split(' '), cwd=folder, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL).wait() != 0:
        raise Exception('Cannot build %s' % folder)


def make_a_game(zip_file):
    dirpath = tempfile.mkdtemp()
    shutil.copy("ConsoleApp1.csproj", dirpath)

    print("Extracting %s" % zip_file)
    subprocess.Popen(["7z", "e", zip_file, "-o%s" % dirpath]).wait()

    if not os.path.exists(dirpath + '/Model'):
        copy_tree('Model', dirpath)

    print("Building %s" % zip_file)
    build(dirpath)

    print("Running %s")

    with threadLock:
        global port
        port += 1
        port_1 = port
        port += 1
        port_2 = port

    nitro = 'true' if mode == 2 or mode == 3 else 'false'
    team_size = '2' if mode == 1 else '3'


    lr = subprocess.Popen([
        local_runner,
        '--seed', '0',
        '--p1', 'tcp-{}'.format(port_1),
        '--p2', 'tcp-{}'.format(port_2),
        '--p1-name', 'current',
        '--p2-name', os.path.basename(zip_file),
        '--noshow',
        '--nitro', nitro,
        '--team-size', team_size,
        '--log-file', os.path.abspath(regression_logs + '/%s.log' % os.path.basename(zip_file)),
        '--results-file', os.path.abspath(regression_logs + '/%s.res' % os.path.basename(zip_file)),
    ])

    subprocess.Popen([
        'dotnet',
        'regression_current_build/ConsoleApp1.dll',
        '127.0.0.1',
        str(port_1),
        '0000000000000000',
        ])
    subprocess.Popen([
        'dotnet',
        '%s/bin/Release/netcoreapp2.1/ConsoleApp1.dll' % dirpath,
        '127.0.0.1',
        str(port_2),
        '0000000000000000',
        ],
        stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)

    lr.wait()

    shutil.rmtree(dirpath)

print('Mode: \n1. 2x2\n2. 2x2 with nitro\n3. 3x3 with nitro')
mode = int(input())

pack_dir = "packs"
local_runner = "codeball2018.exe"

build(".", 'regression_current_build')

regression_logs = 'regressions/%s' % datetime.datetime.now().strftime("%Y-%m-%d_%H-%M-%S")

os.mkdir(regression_logs)


pool = ThreadPool(5)

archives = [pack_dir + '/' + f for f in listdir(pack_dir) if isfile(pack_dir + '/' + f)]
print("Total: " + '|'.join(archives[::-1]))
pool.map(make_a_game, archives[::-1], chunksize=1)

pool.close()
pool.join()

parse_results([regression_logs + '/' + f for f in listdir(regression_logs) if f.endswith('.res')])

input("Press Enter to continue...")
