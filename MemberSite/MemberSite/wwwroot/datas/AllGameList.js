import {Provider} from './Provider';

const dataList = {
    GPI:require('./GPI/GameList.json'),
    YGG:require('./YGG/GameList.json'),
    Joker:require('./Joker/GameList.json'),
    SG:require('./SG/GameList.json'),
    PP: require('./PP/GameList.json'),
    PG: require('./PG/GameList.json'), 
    CQ9: require('./CQ9/GameList.json')
}

export const GamesByProvider = (_provider)=>{
    return dataList[_provider.name].data.filter(function (game) {
  return game.disabled == null || game.disabled != 1;
}).map(game=>{
        return{
            ...game,
            providerName:_provider.name,
            provider:_provider.value, 
        }
    }).sort((a,b) => a.hot&&a.hot ? -1 : 1 );
}

export const AllGames = ()=>{
    let result = [];
    for(let p in Provider){
        let _provider = Provider[p];
        if(_provider.value !== 'All'){
            let games = dataList[_provider.name].data;
            for(let g in games){
                if(games[g].disabled == null || games[g].disabled != 1) {
                    result.push({
                        ...games[g],
                        provider:_provider.value,
                        providerName:_provider.name
                    });
                }
            }
        }
    }

    let aa = [];
    let bb = [];

    result.forEach(element => {
        if(element.hot&&element.hot){
            aa.push(element);
        } else {
            bb.push(element);
        }
    });

    for(let i=0;i<aa.length;i++) {
        let idx = Math.floor(Math.random()*aa.length);
        let tempEle1 = aa[i];
        let tempEle2 = aa[idx];

        aa[i] = tempEle2;
        aa[idx] = tempEle1;
    }

    for(let i=0;i<bb.length;i++) {
        let idx = Math.floor(Math.random()*bb.length);
        let tempEle1 = bb[i];
        let tempEle2 = bb[idx];

        bb[i] = tempEle2;
        bb[idx] = tempEle1;        
    }

    result = [];

    aa.forEach(element => {
        result.push(element);
    });

    bb.forEach(element => {
        result.push(element);
    });    

    return result;
}