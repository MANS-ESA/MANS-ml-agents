import pytest
from unittest import mock

import torch  # noqa I201

from mlagents.torch_utils import set_torch_config, default_device
from mlagents.trainers.settings import TorchSettings


@pytest.mark.parametrize(
    "device_str, expected_type, expected_index, expected_tensor_type",
    [
        ("cpu", "cpu", None, torch.float32),
        ("cuda", "cuda", None, torch.float32),
        ("cuda:42", "cuda", 42, torch.float32),
        ("opengl", "opengl", None, torch.float32),
    ],
)
@mock.patch.object(torch, "set_default_dtype")
def test_set_torch_device(
    mock_set_default_tensor_type,
    device_str,
    expected_type,
    expected_index,
    expected_tensor_type,
):
    try:
        torch_settings = TorchSettings(device=device_str)
        set_torch_config(torch_settings)
        assert default_device().type == expected_type
        if expected_index is None:
            assert default_device().index is None
        else:
            assert default_device().index == expected_index
        mock_set_default_tensor_type.assert_called_once_with(expected_tensor_type)
    except Exception:
        raise
    finally:
        torch_settings = TorchSettings(device=None)
        set_torch_config(torch_settings)
