let str = [...Array(67000)].map(i=>(~~(Math.random()*36)).toString(36)).join('');

console.log(str);

console.log("length : " + str.length);
