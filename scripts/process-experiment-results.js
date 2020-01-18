/*
Usage example:
node ./process-experiment-results.js ./output/MCTOPMTWP-4-pr04-out.txt.log \
 avgScore \
 bestScore \
 avgTime \
 params.sa-seed \
 params.sa-max-iter \
 params.sa-cool-fact \
 params.sa-min-swap \
 params.sa-max-swap \
 params.sa-max-del \
 params.sa-max-ins \
 params.sa-init-sol \
 params.sa-cool-func \
 bestTime \
 bestIters \
 bestSol
*/

const { promisify } = require('util')
const fs = require('fs');
const path = require('path');
const dir = process.cwd();
const mysql = require('mysql')

const fsReadFilePromise = promisify(fs.readFile)
const fsExistsPromise = promisify(fs.exists)
const fsUnlinkPromise = promisify(fs.unlink)
const fsWriteFile = promisify(fs.writeFile)

const conn = mysql.createConnection({
  host: '127.0.0.1',
  port: 3307,
  user: 'root',
  password: 'root',
  database: 'test'
})

const QUERY_DROP_TABLE = `DROP TABLE IF EXISTS results;`
const QUERY_CREATE_TABLE = `
CREATE TABLE results (
  id INT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
  avgScore FLOAT(18, 2) NOT NULL,
  avgTime FLOAT(18, 2) NOT NULL,
  bestScore FLOAT(18, 2) NOT NULL,
  params_sa_seed INT NOT NULL,
  params_sa_max_iter INT NOT NULL,
  params_sa_cool_fact FLOAT(18, 2) NOT NULL,
  params_sa_min_swap INT NOT NULL,
  params_sa_max_swap INT NOT NULL,
  params_sa_max_del INT NOT NULL,
  params_sa_max_ins INT NOT NULL,
  params_sa_init_sol VARCHAR(255) NOT NULL,
  params_sa_cool_func VARCHAR(255) NOT NULL,
  bestTime INT NOT NULL,
  bestIters INT NOT NULL,
  bestSol TEXT NOT NULL
);`
const QUERY_AVG_SCORE = `
SELECT avgScore, avgTime, bestScore, params_sa_seed, params_sa_max_iter, params_sa_cool_fact, params_sa_min_swap, params_sa_max_swap, params_sa_max_del, params_sa_max_ins, params_sa_init_sol, params_sa_cool_func, bestTime, bestIters, bestSol 
FROM results ORDER BY avgScore DESC, avgTime ASC, bestScore DESC LIMIT 10;`
const QUERY_BEST_SCORE = `
SELECT bestScore, avgScore, avgTime, params_sa_seed, params_sa_max_iter, params_sa_cool_fact, params_sa_min_swap, params_sa_max_swap, params_sa_max_del, params_sa_max_ins, params_sa_init_sol, params_sa_cool_func, bestTime, bestIters, bestSol 
FROM results ORDER BY bestScore DESC, avgScore DESC, avgTime ASC LIMIT 10;`
const QUERY_INIT_SOL = `
SELECT params_sa_init_sol, params_sa_seed, AVG(avgScore) as score, AVG(avgTime) as time
FROM results GROUP BY params_sa_init_sol, params_sa_seed ORDER BY score DESC, params_sa_init_sol, time ASC;`
const QUERY_COOL_FUNC = `
SELECT params_sa_cool_func, params_sa_cool_fact, AVG(avgScore) as score, AVG(avgTime) as time
FROM results GROUP BY params_sa_cool_func, params_sa_cool_fact ORDER BY score DESC, params_sa_cool_func, time ASC;`
const QUERY_MIN_SWAP = `
SELECT params_sa_min_swap, AVG(avgScore) as score, AVG(avgTime) as time
FROM results GROUP BY params_sa_min_swap ORDER BY score DESC, time ASC;`
const QUERY_MAX_SWAP = `
SELECT params_sa_max_swap, AVG(avgScore) as score, AVG(avgTime) as time
FROM results GROUP BY params_sa_max_swap ORDER BY score DESC, time ASC;`
const QUERY_SWAP_DIFF = `
SELECT swap_results.swap_diff, AVG(swap_results.avgScore) as score, AVG(swap_results.avgTime) as time FROM (
	SELECT params_sa_max_swap - params_sa_min_swap as swap_diff, avgScore, avgTime
	FROM results
) as swap_results WHERE swap_results.swap_diff >= 0
GROUP BY swap_results.swap_diff
ORDER BY score DESC, time ASC;`
const QUERY_MAX_DEL = `
SELECT params_sa_max_del, AVG(avgScore) as score, AVG(avgTime) as time
FROM results GROUP BY params_sa_max_del ORDER BY score DESC, time ASC;`
const QUERY_MAX_INS = `
SELECT params_sa_max_ins, AVG(avgScore) as score, AVG(avgTime) as time
FROM results GROUP BY params_sa_max_ins ORDER BY score DESC, time ASC;`
const QUERY_DEL_INS_DIFF = `
SELECT del_ins_results.del_ins_diff, AVG(del_ins_results.avgScore) as score, AVG(del_ins_results.avgTime) as time FROM (
	SELECT params_sa_max_del - params_sa_max_ins as del_ins_diff, avgScore, avgTime
	FROM results
) as del_ins_results WHERE del_ins_results.del_ins_diff >= 0
GROUP BY del_ins_results.del_ins_diff
ORDER BY score DESC, time ASC;`
const QUERY_MAX_ITER = `
SELECT params_sa_max_iter, AVG(avgScore) as score, AVG(avgTime) as time
FROM results GROUP BY params_sa_max_iter ORDER BY score DESC, time ASC;`

const mysqlQuery = (conn, stmt) => new Promise((resolve, reject) => conn.query(stmt.trim(), (err, result) => {
  if (err) return reject(err);
  resolve(result)
}))


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
const prepSql = async (records) => {
  const stmts = []

  const fileExists = await fsExistsPromise(sqlOutFile)
  if (fileExists) await fsUnlinkPromise(sqlOutFile)

  records.forEach((val, i) => {
    if (i % 100 == 0) stmts.push([]);
    stmts[stmts.length - 1].push(val);
  });

  const sqlStmts = stmts.map(
    stmt => ('INSERT INTO results (avgScore, avgTime, bestScore, params_sa_seed, params_sa_max_iter, params_sa_cool_fact, params_sa_min_swap, params_sa_max_swap, params_sa_max_del, params_sa_max_ins, params_sa_init_sol, params_sa_cool_func, bestTime, bestIters, bestSol) VALUES\n' +
      stmt.map(val => `(${val['avgScore']},${val['avgTime']},${val['bestScore']},${val['params']['sa-seed']},${val['params']['sa-max-iter']},${val['params']['sa-cool-fact']},${val['params']['sa-min-swap']},${val['params']['sa-max-swap']},${val['params']['sa-max-del']},${val['params']['sa-max-ins']},"${val['params']['sa-init-sol']}","${val['params']['sa-cool-func']}",${val['bestTime']},${val['bestIters']},"${val['bestSol']}")`).join(',\n')).trim()
  );
  await fsWriteFile(sqlOutFile, sqlStmts.join(';\n') + ';\n', { flag: 'w', encoding: 'utf8' });
  return sqlStmts;
}

const prepCsv = async (records, fname) => {
  const fields = Object.keys(records[0])
  const rows = records.map(row => {
    const line = fields.map(key => {
      const data = row[key]
      return typeof data === 'number' ? data : `"${data}"`
    }).join(',')
    return line;
  });

  const content = [
    fields.map(key => `"${key}"`).join(','),
    ...rows
  ].join('\n')
  await fsWriteFile(`${csvOutFile}-${fname}`, content, { flag: 'w', encoding: 'utf8' });
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

const sqlOutFile = `${__dirname}/output/${path.basename(file)}.sql`;
const csvOutFile = `${__dirname}/output/${path.basename(file)}`;

(async () => {
  let rawResults = (await fsReadFilePromise(file)).toString().trim()
  const results = parser(rawResults);

  sort(sortFields, results);
  const stms = await prepSql(results);

  await new Promise((resolve, reject) => conn.connect(err => err ? reject(err) : resolve()))

  await mysqlQuery(conn, QUERY_DROP_TABLE);
  await mysqlQuery(conn, QUERY_CREATE_TABLE);
  await Promise.all(stms.map(sql => mysqlQuery(conn, sql)));

  let qRes = await mysqlQuery(conn, QUERY_AVG_SCORE);
  prepCsv(qRes, 'top-10.csv')
  qRes = await mysqlQuery(conn, QUERY_BEST_SCORE);
  prepCsv(qRes, 'best-10.csv')
  qRes = await mysqlQuery(conn, QUERY_INIT_SOL);
  prepCsv(qRes, 'init-sol.csv')
  qRes = await mysqlQuery(conn, QUERY_COOL_FUNC);
  prepCsv(qRes, 'cool-func.csv')
  qRes = await mysqlQuery(conn, QUERY_MIN_SWAP);
  prepCsv(qRes, 'min-swap.csv')
  qRes = await mysqlQuery(conn, QUERY_MAX_SWAP);
  prepCsv(qRes, 'max-swap.csv')
  qRes = await mysqlQuery(conn, QUERY_SWAP_DIFF);
  prepCsv(qRes, 'swap-diff.csv')
  qRes = await mysqlQuery(conn, QUERY_MAX_DEL);
  prepCsv(qRes, 'max-del.csv')
  qRes = await mysqlQuery(conn, QUERY_MAX_INS);
  prepCsv(qRes, 'max-ins.csv')
  qRes = await mysqlQuery(conn, QUERY_DEL_INS_DIFF);
  prepCsv(qRes, 'del-ins-diff.csv')
  qRes = await mysqlQuery(conn, QUERY_MAX_ITER);
  prepCsv(qRes, 'max-iter.csv')

  await new Promise((resolve, reject) => conn.end(err => err ? reject(err) : resolve()))
})();