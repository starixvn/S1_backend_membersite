export class GameElem extends React.Component{
    constructor(props){
        super(props);
        this.state = {IsFavorite: this.props.IsFavorite===true}
        this.HandleFavourite = this.HandleFavourite.bind(this);
    }
    HandleFavourite=()=>{
        this.setState({
            IsFavorite: !this.state.IsFavorite
        });

        this.props.HandleFavourite();
    }
    PlayNow=()=>{
        if(isLogin==='1'){
            let tpId = this.props.ProviderId;
            let gameId = this.props.GameId;
            window.open(`${entPageUrl}&fun=${0}&gameId=${gameId}&tpId=${tpId}&gameType=slots`, `Tp_${tpId}_${gameId}`, 'toolbar=no, menubar=no,scrollbars=no,resizable=no,location=no,directories=no,status=no').focus()
        }else{
            ShowLogin();
        }
    }
    PlayFree=()=>{
        if(isLogin==='1'){
            let tpId = this.props.ProviderId;
            let gameId = this.props.GameId;
            window.open(`${entPageUrl}&fun=${1}&gameId=${gameId}&tpId=${tpId}&gameType=slots`, `Tp_${tpId}_${gameId}`, 'toolbar=no, menubar=no,scrollbars=no,resizable=no,location=no,directories=no,status=no').focus()
        }else{
            ShowLogin();
        }
    }
    render(){
        let elem = (
            <>
                <div>
                    <span>{this.props.GameName}</span>
                    {
                        this.state.IsFavorite === true ? <span>★</span> : null
                    }
                </div>
                <div className="playNow" style={{display : 'none'}}>
                    <a onClick={this.PlayNow}>
                        <span>PLAY NOW</span>
                    </a>
                    <a onClick={this.PlayFree}>
                        <span>FREE</span>
                    </a>
                    <a onClick={this.HandleFavourite}>
                        <span>{this.state.IsFavorite === true ? '★' : '☆' }</span>
                    </a>
                </div>
            </>
        );

        return this.props.BigItem ? (
                <div style={{background: `url(/common/images/Casino/${this.props.ProviderName}/${this.props.ImgUrl})no-repeat top center / cover`}} className="BIG">
                    {elem}
                </div>
            ) : (
            <div style={{background: `url(/common/images/Casino/${this.props.ProviderName}/${this.props.ImgUrl})no-repeat top center / cover`}}>
                {elem}
            </div>
        )
    }
}