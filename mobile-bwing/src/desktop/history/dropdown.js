export class DropdownBox extends React.Component {
	constructor(props) {
		super(props);
		this.state = { isOpen: false };
		this.ClickDropdown = this.ClickDropdown.bind(this);
		this.handleClickOutside = this.handleClickOutside.bind(this);
		this.SelectItem = this.SelectItem.bind(this);
		document.addEventListener("mousedown", this.handleClickOutside);
	}
	handleClickOutside(e) {
		if (this.wrapper && !this.wrapper.contains(e.target)) {
			this.setState({ isOpen: false });
		}
	}
	SetOuterDivRef = node => {
		this.wrapper = node;
	};
	ClickDropdown = () => {
		this.setState({ isOpen: !this.state.isOpen });
	};
	SelectItem = v => {
		this.setState({ isOpen: false });
		if (this.props.OnSelectItem !== undefined) {
			this.props.OnSelectItem(
				v,
				this.props.DataColumn,
				this.props.BindingValue
			);
		}
	};
	render() {
		let rows =
			this.props.Options &&
			this.props.Options.map(item => {
				return (
					<div
						key={`dp-${item.value}`}
						className="item"
						onClick={this.SelectItem.bind(null, item)}
						data-selected={
							this.props.SelectedItem !== undefined &&
							this.props.SelectedItem !== null &&
							this.props.SelectedItem.value === item.value
						}>
						<span className="text">{item.displayText}</span>
					</div>
				);
			});

		return (
			<div className="wallet-group dropdown" ref={this.SetOuterDivRef}>
				<div className="selected-text" onClick={this.ClickDropdown}>
					<span className="text-bold datelist">
						{this.props.SelectedItem
							? this.props.SelectedItem.displayText
							: ""}
					</span>
					<span className="icon-action">
						<i className="icon icon-arrow-down"></i>
					</span>
				</div>
				<div
					className={`dropdown-container${
						this.state.isOpen ? "" : " hide"
					}`}>
					{rows}
				</div>
			</div>
		);
	}
}
