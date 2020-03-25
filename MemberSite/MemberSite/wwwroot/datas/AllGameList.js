import {Provider} from './Provider';

const dataList = {
    GPI:require('./GPI/GameList.json')
}

export const GamesByProvider = (_provider)=>{
    return [{
        providerName:_provider.name,
        provider:_provider.value, 
        data:dataList[_provider.displayText].data
    }];
}

export const AllGames = ()=>{
    let result = Object.keys(Provider).filter(key=>{
        return Provider[key].value !== 'All';
    }).map(key=> {
        return {
            providerName:Provider[key].name,
            provider:Provider[key].value,
            data:dataList[Provider[key].displayText].data
        }
    });

    return result;
}