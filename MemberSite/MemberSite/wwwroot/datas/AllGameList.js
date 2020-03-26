import {Provider} from './Provider';

const dataList = {
    GPI:require('./GPI/GameList.json')
}

export const GamesByProvider = (_provider)=>{
    return dataList[_provider.name].data.map(game=>{
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
                result.push({
                    ...games[g],
                    provider:_provider.value,
                    providerName:_provider.name
                })
            }
        }
    }

    result.sort((a,b) => a.hot&&a.hot ? -1 : 1 );

    return result;
}