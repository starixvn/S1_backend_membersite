
(function ($, React, ReactDOM, i18n) {

    const MobileAndTabletCheck = ()=>{
        let check = false;
        (function(a){if(/(android|bb\d+|meego).+mobile|avantgo|bada\/|blackberry|blazer|compal|elaine|fennec|hiptop|iemobile|ip(hone|od)|iris|kindle|lge |maemo|midp|mmp|mobile.+firefox|netfront|opera m(ob|in)i|palm( os)?|phone|p(ixi|re)\/|plucker|pocket|psp|series(4|6)0|symbian|treo|up\.(browser|link)|vodafone|wap|windows ce|xda|xiino|android|ipad|playbook|silk/i.test(a)||/1207|6310|6590|3gso|4thp|50[1-6]i|770s|802s|a wa|abac|ac(er|oo|s\-)|ai(ko|rn)|al(av|ca|co)|amoi|an(ex|ny|yw)|aptu|ar(ch|go)|as(te|us)|attw|au(di|\-m|r |s )|avan|be(ck|ll|nq)|bi(lb|rd)|bl(ac|az)|br(e|v)w|bumb|bw\-(n|u)|c55\/|capi|ccwa|cdm\-|cell|chtm|cldc|cmd\-|co(mp|nd)|craw|da(it|ll|ng)|dbte|dc\-s|devi|dica|dmob|do(c|p)o|ds(12|\-d)|el(49|ai)|em(l2|ul)|er(ic|k0)|esl8|ez([4-7]0|os|wa|ze)|fetc|fly(\-|_)|g1 u|g560|gene|gf\-5|g\-mo|go(\.w|od)|gr(ad|un)|haie|hcit|hd\-(m|p|t)|hei\-|hi(pt|ta)|hp( i|ip)|hs\-c|ht(c(\-| |_|a|g|p|s|t)|tp)|hu(aw|tc)|i\-(20|go|ma)|i230|iac( |\-|\/)|ibro|idea|ig01|ikom|im1k|inno|ipaq|iris|ja(t|v)a|jbro|jemu|jigs|kddi|keji|kgt( |\/)|klon|kpt |kwc\-|kyo(c|k)|le(no|xi)|lg( g|\/(k|l|u)|50|54|\-[a-w])|libw|lynx|m1\-w|m3ga|m50\/|ma(te|ui|xo)|mc(01|21|ca)|m\-cr|me(rc|ri)|mi(o8|oa|ts)|mmef|mo(01|02|bi|de|do|t(\-| |o|v)|zz)|mt(50|p1|v )|mwbp|mywa|n10[0-2]|n20[2-3]|n30(0|2)|n50(0|2|5)|n7(0(0|1)|10)|ne((c|m)\-|on|tf|wf|wg|wt)|nok(6|i)|nzph|o2im|op(ti|wv)|oran|owg1|p800|pan(a|d|t)|pdxg|pg(13|\-([1-8]|c))|phil|pire|pl(ay|uc)|pn\-2|po(ck|rt|se)|prox|psio|pt\-g|qa\-a|qc(07|12|21|32|60|\-[2-7]|i\-)|qtek|r380|r600|raks|rim9|ro(ve|zo)|s55\/|sa(ge|ma|mm|ms|ny|va)|sc(01|h\-|oo|p\-)|sdk\/|se(c(\-|0|1)|47|mc|nd|ri)|sgh\-|shar|sie(\-|m)|sk\-0|sl(45|id)|sm(al|ar|b3|it|t5)|so(ft|ny)|sp(01|h\-|v\-|v )|sy(01|mb)|t2(18|50)|t6(00|10|18)|ta(gt|lk)|tcl\-|tdg\-|tel(i|m)|tim\-|t\-mo|to(pl|sh)|ts(70|m\-|m3|m5)|tx\-9|up(\.b|g1|si)|utst|v400|v750|veri|vi(rg|te)|vk(40|5[0-3]|\-v)|vm40|voda|vulc|vx(52|53|60|61|70|80|81|83|85|98)|w3c(\-| )|webc|whit|wi(g |nc|nw)|wmlb|wonu|x700|yas\-|your|zeto|zte\-/i.test(a.substr(0,4))) check = true;})(navigator.userAgent||navigator.vendor||window.opera);
        return check;
    };

    class Panel extends React.Component {
        constructor(props){
            super(props);
            this.state = {datas: [], hover:false, pos:null, initWidth:0};
            this.ClickMarquee = this.ClickMarquee.bind(this);
            this.GetAnnouncement = this.GetAnnouncement.bind(this);
        }
        componentDidMount(){
            this.GetAnnouncement();
            this.intervalId = setInterval(this.GetAnnouncement, 60000);
        }
        SetPos=()=>{
            if(this.state.prev!==null){
                let outerBox = $(this.refs.content).width();
                this.setState((prev)=>{
                    if(prev.pos >= outerBox){
                        return {pos:-prev.initWidth}
                    }else{
                        return  {pos:prev.pos+1.5}
                    }
                });
            }
        }
        componentDidUpdate=()=>{
            if(this.state.pos === null){
                let pos = $(this.refs.marquee).width();
                this.setState({pos:-pos, initWidth:pos});
            }
        }
        GetAnnouncement(){
            axios.post(`/api/Message/GetAnnouncement/${SiteLanguage}`,{}).then(res=>{
                let resData = res.data.data;
                let stateData = this.state.datas;
                let upd = false;
                if(resData.length !== 0 && stateData.length !== 0){
                    for(let d in resData){
                        let find = stateData.find(x=> x.id === resData[d].id 
                            && x.subject===resData[d].subject
                            && x.content===resData[d].content
                        );

                        if(!find){
                            upd = true;
                            break;
                        }
                    }
                }else{
                    upd = true;
                }

                if(upd){
                    if(this.intervalId_Pos === undefined){
                        this.intervalId_Pos = setInterval(this.SetPos, 25);
                    }

                    this.setState({
                        datas:res.data.data.map(item=>{
                            return {
                                ...item,
                                subject: item.subject,
                                content: item.content
                            }
                        })
                    });
                }
            });
        }
        ClickMarquee(item){
            if(announcementUrl){
                if(MobileAndTabletCheck()){
                    let content = "";
                    item.content.split('\\n').forEach((c)=>{
                        content += `<p>${c}</p>`;
                    });

                    $("#AnnouncementDialog").attr('class', 'modal modalOpen');
                    $("#AnnouncementDialog").attr('data-open', 'true');
                    $("#AnnouncementDialogTitle").html(item.subject);
                    $("#AnnouncementDialogContent").html(`<div>${content}</div>`);
                }else{
                    window.location.href = announcementUrl;
                }
            }else{
                ShowLogin();
            }
        }
        MarqueeOnMouseOver=()=>{
            clearInterval(this.intervalId_Pos);
        }
        MarqueeOnMouseOut=()=>{
            clearInterval(this.intervalId_Pos);
            this.intervalId_Pos = setInterval(this.SetPos, 25);
        }
        render () {
            let rows = this.state.datas.map((item)=>{
                
                return (
                    <button
                        onMouseOut={this.MarqueeOnMouseOut}
                        onMouseOver={this.MarqueeOnMouseOver} key={`announcement-${item.id}`} style={{'paddingLeft':'2rem'}} onClick={this.ClickMarquee.bind(this, item)}>{item.subject}</button>
                )
            });

            let style = this.state.pos === null ? {} : {right:this.state.pos};
            return(
                <a className="marquee">
                    <div>
                        <i className="icon icon-volume-on"></i>
                    </div>
                    <div ref="content" className="marquee-box">
                        <div className="marquee-run" ref='marquee' style={style} >
                            { rows }
                        </div>
                    </div>
                    <div>●●●</div>
                </a>
            );
        }
    }

    ReactDOM.render(<Panel/>,document.getElementById('Marquee'));
})(jQuery, React, ReactDOM, i18n);