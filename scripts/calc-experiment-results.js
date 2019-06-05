// Usage: node calc-experiment-results.js file1 file2...
const fs = require('fs');
const path = require('path');
const dir = process.cwd();

/**
 * @param {string} raw
 * @returns {{
 *    params: {
        'sa-seed': Number,
        'sa-max-iter': Number,
        'sa-cool-fact': Number,
        'sa-min-swap': Number,
        'sa-max-swap': Number,
        'sa-max-del': Number,
        'sa-max-ins': Number,
        'sa-init-sol': string,
        'sa-cool-func': string
      },
      avgScore: Number,
      avgTime: Number,
      bestScore: Number,
      bestTime: Number,
      bestIters: Number,
      bestSol: string
    }[]}
 */
const parser = (raw) => {
  return raw.split("\n")
    .filter(line => !line.startsWith("Permutation"))
    .map(line => {
      let [params, avgScore, avgTime, bestScore, bestTime, bestIters, bestSol] = line.split(";");
      params = JSON.parse(params);
      params['sa-seed'] = parseInt(params['sa-seed']);
      params['sa-max-iter'] = parseInt(params['sa-max-iter']);
      params['sa-cool-fact'] = parseFloat(params['sa-cool-fact']);
      params['sa-min-swap'] = parseInt(params['sa-min-swap']);
      params['sa-max-swap'] = parseInt(params['sa-max-swap']);
      params['sa-max-del'] = parseInt(params['sa-max-del']);
      params['sa-max-ins'] = parseInt(params['sa-max-ins']);

      return {
        params: params,
        avgScore: parseFloat(avgScore),
        avgTime: parseFloat(avgTime),
        bestScore: parseInt(bestScore),
        bestTime: parseInt(bestTime),
        bestIters: parseInt(bestIters),
        bestSol: bestSol
      };
    });
}

/**
 * @param {{
 *    params: {
        'sa-seed': Number,
        'sa-max-iter': Number,
        'sa-cool-fact': Number,
        'sa-min-swap': Number,
        'sa-max-swap': Number,
        'sa-max-del': Number,
        'sa-max-ins': Number,
        'sa-init-sol': string,
        'sa-cool-func': string
      },
      avgScore: Number,
      avgTime: Number,
      bestScore: Number,
      bestTime: Number,
      bestIters: Number,
      bestSol: string
    }[]} records
 */
const csv = (records) => {
  const csvFile = `${__dirname}/output/${path.basename(file)}.csv`;
  let content = `avgScore;avgTime;bestScore;params.sa-seed;params.sa-max-iter;params.sa-cool-fact;params.sa-min-swap;params.sa-max-swap;params.sa-max-del;params.sa-max-ins;params.sa-init-sol;params.sa-cool-func;bestTime;bestIters;bestSol\n`;
  records.forEach(val => {
    content += `${val['avgScore']};${val['avgTime']};${val['bestScore']};${val['params']['sa-seed']};${val['params']['sa-max-iter']};${val['params']['sa-cool-fact']};${val['params']['sa-min-swap']};${val['params']['sa-max-swap']};${val['params']['sa-max-del']};${val['params']['sa-max-ins']};${val['params']['sa-init-sol']};${val['params']['sa-cool-func']};${val['bestTime']};${val['bestIters']};${val['bestSol']}\n`
  });
  return new Promise((resolve, reject) => fs.writeFile(csvFile, content, { flag: 'w', encoding: 'utf8' }, err => err ? reject(err) : resolve(true)));
}

/**
 * @param {string[]} keys 
 * @param {{
 *    params: {
        'sa-seed': Number,
        'sa-max-iter': Number,
        'sa-cool-fact': Number,
        'sa-min-swap': Number,
        'sa-max-swap': Number,
        'sa-max-del': Number,
        'sa-max-ins': Number,
        'sa-init-sol': string,
        'sa-cool-func': string
      },
      avgScore: Number,
      avgTime: Number,
      bestScore: Number,
      bestTime: Number,
      bestIters: Number,
      bestSol: string
    }[]} records
 */
const sort = (keys, records) => {
  records.sort((a, b) => {
    for (let i = 0; i < keys.length; i++) {
      const key = keys[i];
      let valA = a, valB = b;
      key.split(".").forEach(k => {
        valA = valA[k];
        valB = valB[k];
      });

      if (valA < valB) return 1;
      if (valA > valB) return -1;
    }

    return 0;
  });
}

// Main

let file = process.argv[2];
if (file.startsWith('./'))
  file = `${dir}/${file.replace('./', '')}`;
if (!file.startsWith('/') && !file.startsWith('~') && !file.startsWith('ftp') && !file.startsWith('http'))
  file = `${dir}/${file}`;
const sortFields = process.argv.slice(3);

(async () => {
  let rawResults = await new Promise((resolve, reject) => {
    fs.readFile(file, (err, data) => err ? reject(err) : resolve(data.toString().trim()));
  });
  const results = parser(rawResults);

  sort(sortFields, results);
  await csv(results);
})();